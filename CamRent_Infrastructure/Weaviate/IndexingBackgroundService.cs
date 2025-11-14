using System.Threading.Channels;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CamRent_Infrastructure.Weaviate
{
	public sealed class IndexingBackgroundService : BackgroundService, IIndexingService
	{
		private readonly Channel<(string op, string cls, Guid id)> _channel = Channel.CreateUnbounded<(string, string, Guid)>();
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<IndexingBackgroundService> _logger;

		public IndexingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<IndexingBackgroundService> logger)
		{
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		public void EnqueueUpsert(string @class, Guid id) => _channel.Writer.TryWrite(("upsert", @class, id));
		public void EnqueueDelete(string @class, Guid id) => _channel.Writer.TryWrite(("delete", @class, id));

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var (op, cls, id) = await _channel.Reader.ReadAsync(stoppingToken);
					using var scope = _scopeFactory.CreateScope();
					var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
					var embed = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
					var store = scope.ServiceProvider.GetRequiredService<IVectorStore>();
					var client = scope.ServiceProvider.GetRequiredService<IWeaviateClient>();

					if (op == "delete")
					{
						await client.DeleteAsync(cls, id, stoppingToken);
						continue;
					}

					if (cls == "Camera")
					{
						var c = await uow.Repository<Camera>().GetByIdAsync(id);
						if (c == null) continue;
						var props = new Dictionary<string, object>
						{
							{ "name", $"{c.Brand} {c.Model} {c.Variant}".Trim() },
							{ "description", BuildCameraDescription(c) },
							{ "category", "Camera" },
							{ "priceInfo", $"BaseDailyRate: {c.BaseDailyRate:N0} VND, Deposit%: {c.DepositPercent}" },
							{ "priceDaily", (double)c.BaseDailyRate }
						};
						var text = $"{props["name"]}. {props["description"]}. {props["category"]}. {props["priceInfo"]}";
						var vec = await embed.EmbedAsync(text, stoppingToken);
						await store.UpsertAsync(new VectorUpsertItem { Id = id, Class = "Camera", Properties = props, Vector = vec }, stoppingToken);
					}
					else if (cls == "Accessory")
					{
						var a = await uow.Repository<Accessory>().GetByIdAsync(id);
						if (a == null) continue;
						var props = new Dictionary<string, object>
						{
							{ "name", $"{a.Brand} {a.Model} {a.Variant}".Trim() },
							{ "description", BuildAccessoryDescription(a) },
							{ "category", "Accessory" },
							{ "priceInfo", $"BaseDailyRate: {a.BaseDailyRate:N0} VND, Deposit%: {a.DepositPercent}" },
							{ "priceDaily", (double)a.BaseDailyRate }
						};
						var text = $"{props["name"]}. {props["description"]}. {props["category"]}. {props["priceInfo"]}";
						var vec = await embed.EmbedAsync(text, stoppingToken);
						await store.UpsertAsync(new VectorUpsertItem { Id = id, Class = "Accessory", Properties = props, Vector = vec }, stoppingToken);
					}
					else if (cls == "Combo")
					{
						var combo = await uow.Repository<Combo>().GetByIdAsync(id);
						if (combo == null) continue;
						var props = new Dictionary<string, object>
						{
							{ "name", combo.Name },
							{ "description", combo.Description ?? string.Empty },
							{ "category", "Combo" },
							{ "priceInfo", combo.PriceOverride.HasValue ? $"ComboPrice: {combo.PriceOverride.Value:N0} VND" : "ComboPrice: N/A" },
							{ "priceDaily", combo.PriceOverride.HasValue ? (double)combo.PriceOverride.Value : 0.0 }
						};
						var text = $"{props["name"]}. {props["description"]}. {props["category"]}. {props["priceInfo"]}";
						var vec = await embed.EmbedAsync(text, stoppingToken);
						await store.UpsertAsync(new VectorUpsertItem { Id = id, Class = "Combo", Properties = props, Vector = vec }, stoppingToken);
					}
				}
				catch (OperationCanceledException) { }
				catch (Exception ex)
				{
					_logger.LogError(ex, "Indexing job failed");
					// simple backoff
					await Task.Delay(1000, stoppingToken);
				}
			}
		}

		private static string BuildCameraDescription(Camera c)
		{
			var parts = new List<string> { $"Brand: {c.Brand}", $"Model: {c.Model}" };
			if (!string.IsNullOrWhiteSpace(c.Variant)) parts.Add($"Variant: {c.Variant}");
			if (!string.IsNullOrWhiteSpace(c.SpecsJson)) parts.Add($"Specs: {c.SpecsJson}");
			return string.Join(". ", parts);
		}

		private static string BuildAccessoryDescription(Accessory a)
		{
			var parts = new List<string> { $"Brand: {a.Brand}", $"Model: {a.Model}" };
			if (!string.IsNullOrWhiteSpace(a.Variant)) parts.Add($"Variant: {a.Variant}");
			if (!string.IsNullOrWhiteSpace(a.SpecsJson)) parts.Add($"Specs: {a.SpecsJson}");
			return string.Join(". ", parts);
		}
	}
}

