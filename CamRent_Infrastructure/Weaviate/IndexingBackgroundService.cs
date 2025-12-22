using System.Threading.Channels;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
						// Background service này được gọi khi combo được tạo/cập nhật/xóa
						// Mục đích: Index lại combo vào Weaviate với thông tin mới nhất

						// Bước 1: Lấy combo kèm theo tất cả Items và thông tin Camera/Accessory của từng item
						// Sử dụng ThenInclude để load nested entities (Combo -> Items -> Camera/Accessory)
						// Điều này đảm bảo khi combo thay đổi (thêm/xóa item), ta có thông tin đầy đủ để index
						var combos = await uow.Repository<Combo>().ListAsync(
							filter: c => c.Id == id,
							include: c => c.Include(combo => combo.Items)
								.ThenInclude(item => item.Camera)
								.Include(combo => combo.Items)
								.ThenInclude(item => item.Accessory));
						var combo = combos.FirstOrDefault();
						if (combo == null) continue;

						// Bước 2: Build danh sách mô tả chi tiết cho từng item trong combo
						// Mỗi ComboItem có thể là Camera hoặc Accessory, ta lấy thông tin brand/model/variant
						// Thông tin này giúp AI search tìm được combo dựa trên thiết bị cụ thể bên trong
						// Ví dụ: User search "combo có Canon R5" sẽ match được combo chứa camera Canon EOS R5
						var itemDescriptions = combo.Items.Select(i =>
						{
							// Nếu là Camera, lấy Brand + Model + Variant
							if (i.CameraId.HasValue && i.Camera != null)
							{
								return $"{i.Camera.Brand} {i.Camera.Model} {i.Camera.Variant}".Trim();
							}
							// Nếu là Accessory, lấy Brand + Model + Variant
							if (i.AccessoryId.HasValue && i.Accessory != null)
							{
								return $"{i.Accessory.Brand} {i.Accessory.Model} {i.Accessory.Variant}".Trim();
							}
							// Fallback nếu không xác định được
							return "Unknown Item";
						});

						// Bước 3: Tạo description đầy đủ cho combo
						// Kết hợp description của combo (hoặc name nếu không có description) với danh sách items
						// Format: "Combo description. Items: Item1, Item2, Item3"
						var baseDesc = !string.IsNullOrWhiteSpace(combo.Description) ? combo.Description : combo.Name;
						var desc = $"{baseDesc}. Items: {string.Join(", ", itemDescriptions)}";

						// Bước 4: Tạo properties object để lưu vào Weaviate
						// Properties này được dùng cho hybrid search (kết hợp keyword và semantic)
						var props = new Dictionary<string, object>
						{
							{ "name", combo.Name },
							{ "description", desc }, // Description chi tiết với thông tin items
							{ "category", "Combo" },
							{ "priceInfo", combo.PriceOverride.HasValue ? $"ComboPrice: {combo.PriceOverride.Value:N0} VND" : "ComboPrice: N/A" },
							{ "priceDaily", combo.PriceOverride.HasValue ? (double)combo.PriceOverride.Value : 0.0 } // Số để filter/sort
						};

						// Bước 5: Tạo text để embedding và generate vector
						// Vector embedding được tạo từ text này để hỗ trợ semantic search
						var text = $"{props["name"]}. {props["description"]}. {props["category"]}. {props["priceInfo"]}";
						var vec = await embed.EmbedAsync(text, stoppingToken);

						// Bước 6: Upsert vào Weaviate
						// Upsert sẽ tạo mới hoặc cập nhật nếu combo đã được index trước đó
						// Điều này đảm bảo thông tin trong vector DB luôn đồng bộ với database
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

