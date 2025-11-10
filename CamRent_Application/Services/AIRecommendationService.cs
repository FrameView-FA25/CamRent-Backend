using System.Text.Json;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public sealed class AIRecommendationService : IAIRecommendationService
	{
		private readonly IUnitOfWork _uow;
		private readonly IVectorStore _vector;

		public AIRecommendationService(IUnitOfWork uow, IVectorStore vector)
		{
			_uow = uow;
			_vector = vector;
		}

		public async Task ReindexAllAsync(CancellationToken ct = default)
		{
			await _vector.EnsureSchemaAsync(ct);

			// Cameras
			var cameras = await _uow.Repository<Camera>().GetAllAsync();
			foreach (var c in cameras)
			{
				var props = new Dictionary<string, object>
				{
					{ "name", $"{c.Brand} {c.Model} {c.Variant}".Trim() },
					{ "description", BuildCameraDescription(c) },
					{ "category", "Camera" },
					{ "priceInfo", $"BaseDailyRate: {c.BaseDailyRate:N0} VND, Deposit%: {c.DepositPercent}" }
				};
				await _vector.UpsertAsync(new VectorUpsertItem { Id = c.Id, Class = "Camera", Properties = props }, ct);
			}

			// Accessories
			var accessories = await _uow.Repository<Accessory>().GetAllAsync();
			foreach (var a in accessories)
			{
				var props = new Dictionary<string, object>
				{
					{ "name", $"{a.Brand} {a.Model} {a.Variant}".Trim() },
					{ "description", BuildAccessoryDescription(a) },
					{ "category", "Accessory" },
					{ "priceInfo", $"BaseDailyRate: {a.BaseDailyRate:N0} VND, Deposit%: {a.DepositPercent}" }
				};
				await _vector.UpsertAsync(new VectorUpsertItem { Id = a.Id, Class = "Accessory", Properties = props }, ct);
			}

			// Combos
			var combos = await _uow.Repository<Combo>().GetAllAsync();
			foreach (var combo in combos)
			{
				var items = await _uow.Repository<ComboItem>().ListAsync(i => i.ComboId == combo.Id);
				var desc = $"{combo.Description}\nItems: " + string.Join(", ", items.Select(i =>
				{
					if (i.CameraId.HasValue) return $"Camera x{i.Quantity}";
					if (i.AccessoryId.HasValue) return $"Accessory x{i.Quantity}";
					return $"Item x{i.Quantity}";
				}));

				var props = new Dictionary<string, object>
				{
					{ "name", combo.Name },
					{ "description", desc },
					{ "category", "Combo" },
					{ "priceInfo", combo.PriceOverride.HasValue ? $"ComboPrice: {combo.PriceOverride.Value:N0} VND" : "ComboPrice: N/A" }
				};
				await _vector.UpsertAsync(new VectorUpsertItem { Id = combo.Id, Class = "Combo", Properties = props }, ct);
			}
		}

		public async Task<IReadOnlyList<VectorSearchResult>> RecommendAsync(string query, int topK = 5, CancellationToken ct = default)
		{
			var results = new List<VectorSearchResult>();
			var classes = new[] { "Camera", "Accessory", "Combo" };

			foreach (var cls in classes)
			{
				var res = await _vector.SearchAsync(query, cls, topK, ct);
				results.AddRange(res);
			}

			return results
				.OrderByDescending(r => r.Score ?? 0)
				.Take(topK)
				.ToList();
		}

		private static string BuildCameraDescription(Camera c)
		{
			var parts = new List<string>
			{
				$"Brand: {c.Brand}",
				$"Model: {c.Model}"
			};
			if (!string.IsNullOrWhiteSpace(c.Variant)) parts.Add($"Variant: {c.Variant}");
			if (!string.IsNullOrWhiteSpace(c.SpecsJson))
			{
				parts.Add($"Specs: {c.SpecsJson}");
			}
			return string.Join(". ", parts);
		}

		private static string BuildAccessoryDescription(Accessory a)
		{
			var parts = new List<string>
			{
				$"Brand: {a.Brand}",
				$"Model: {a.Model}"
			};
			if (!string.IsNullOrWhiteSpace(a.Variant)) parts.Add($"Variant: {a.Variant}");
			if (!string.IsNullOrWhiteSpace(a.SpecsJson))
			{
				parts.Add($"Specs: {a.SpecsJson}");
			}
			return string.Join(". ", parts);
		}
	}
}

 