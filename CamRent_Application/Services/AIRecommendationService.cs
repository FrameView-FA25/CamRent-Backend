using System.Text.Json;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CamRent_Application.Services
{
	public sealed class AIRecommendationService : IAIRecommendationService
	{
		private readonly IUnitOfWork _uow;
		private readonly IVectorStore _vector;
		private readonly IEmbeddingService _embed;

		public AIRecommendationService(IUnitOfWork uow, IVectorStore vector, IEmbeddingService embed)
		{
			_uow = uow;
			_vector = vector;
			_embed = embed;
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
				var text = $"{props["name"]}. {props["description"]}. {props["category"]}. {props["priceInfo"]}";
				var vec = await _embed.EmbedAsync(text, ct);
				await _vector.UpsertAsync(new VectorUpsertItem { Id = c.Id, Class = "Camera", Properties = props, Vector = vec }, ct);
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
				var text = $"{props["name"]}. {props["description"]}. {props["category"]}. {props["priceInfo"]}";
				var vec = await _embed.EmbedAsync(text, ct);
				await _vector.UpsertAsync(new VectorUpsertItem { Id = a.Id, Class = "Accessory", Properties = props, Vector = vec }, ct);
			}

			// Combos
			// Index tất cả combo vào vector database với thông tin chi tiết về các items bên trong
			var combos = await _uow.Repository<Combo>().GetAllAsync();
			foreach (var combo in combos)
			{
				// Bước 1: Lấy tất cả ComboItem của combo này, kèm theo thông tin Camera và Accessory
				// Sử dụng Include để eager load related entities, tránh N+1 query problem
				// Mỗi ComboItem có thể chứa Camera hoặc Accessory (không thể có cả 2)
				var items = await _uow.Repository<ComboItem>().ListAsync(
					filter: i => i.ComboId == combo.Id,
					include: i => i.Include(item => item.Camera).Include(item => item.Accessory));

				// Bước 2: Build danh sách mô tả chi tiết cho từng item trong combo
				// Thay vì chỉ ghi "Camera" hoặc "Accessory", ta lấy thông tin brand/model/variant cụ thể
				// Ví dụ: "Canon EOS R5" thay vì chỉ "Camera"
				var itemDescriptions = items.Select(i =>
				{
					// Nếu item là Camera, lấy thông tin Brand + Model + Variant
					if (i.CameraId.HasValue && i.Camera != null)
					{
						return $"{i.Camera.Brand} {i.Camera.Model} {i.Camera.Variant}".Trim();
					}
					// Nếu item là Accessory, lấy thông tin Brand + Model + Variant
					if (i.AccessoryId.HasValue && i.Accessory != null)
					{
						return $"{i.Accessory.Brand} {i.Accessory.Model} {i.Accessory.Variant}".Trim();
					}
					// Fallback nếu không xác định được loại item
					return "Unknown Item";
				});

				// Bước 3: Tạo description đầy đủ cho combo
				// Sử dụng Description của combo nếu có, nếu không thì dùng Name làm fallback
				// Kết hợp với danh sách items chi tiết để AI có thể search tốt hơn
				// Ví dụ: "Combo chụp ảnh cưới. Items: Canon EOS R5, Canon RF 24-70mm f/2.8, Canon Speedlite 600EX"
				var baseDesc = !string.IsNullOrWhiteSpace(combo.Description) ? combo.Description : combo.Name;
				var desc = $"{baseDesc}. Items: {string.Join(", ", itemDescriptions)}";

				// Bước 4: Tạo properties object để lưu vào Weaviate
				// Properties này sẽ được lưu cùng với vector embedding để hỗ trợ hybrid search
				var props = new Dictionary<string, object>
				{
					{ "name", combo.Name },
					{ "description", desc }, // Description chi tiết với thông tin items
					{ "category", "Combo" },
					{ "priceInfo", combo.PriceOverride.HasValue ? $"ComboPrice: {combo.PriceOverride.Value:N0} VND" : "ComboPrice: N/A" }
				};

				// Bước 5: Tạo text để embedding
				// Kết hợp tất cả thông tin thành một chuỗi text để tạo vector embedding
				// Vector này sẽ được dùng cho semantic search
				var text = $"{props["name"]}. {props["description"]}. {props["category"]}. {props["priceInfo"]}";
				var vec = await _embed.EmbedAsync(text, ct);

				// Bước 6: Lưu vào Weaviate vector database
				// Upsert sẽ tạo mới hoặc cập nhật nếu đã tồn tại
				await _vector.UpsertAsync(new VectorUpsertItem { Id = combo.Id, Class = "Combo", Properties = props, Vector = vec }, ct);
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

 