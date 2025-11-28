using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CamRent_Infrastructure.Weaviate
{
	public interface IWeaviateClient
	{
		Task EnsureSchemaAsync(CancellationToken ct = default);
		Task UpsertAsync(string @class, Guid id, object properties, float[]? vector = null, CancellationToken ct = default);
		Task<WeaviateQueryResult> NearTextAsync(string @class, string query, int limit = 5, CancellationToken ct = default);
		Task<WeaviateQueryResult> NearVectorAsync(string @class, float[] vector, int limit = 5, CancellationToken ct = default);
		Task<WeaviateQueryResult> HybridAsync(string @class, string query, float[]? vector, int limit = 5, float alpha = 0.5f, CancellationToken ct = default);
		Task DeleteAsync(string @class, Guid id, CancellationToken ct = default);
	}

	internal sealed class WeaviateClient : IWeaviateClient
	{
		private readonly HttpClient _http;
		private readonly WeaviateOptions _options;
		private readonly ILogger<WeaviateClient> _logger;
		private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

		public WeaviateClient(HttpClient http, IOptions<WeaviateOptions> options, ILogger<WeaviateClient> logger)
		{
			_http = http;
			_options = options.Value;
			_logger = logger;
		}

		public async Task EnsureSchemaAsync(CancellationToken ct = default)
		{
			var classes = new[] { "Camera", "Accessory", "Combo" };
			foreach (var c in classes)
			{
				await EnsureClassAsync(c, ct);
			}
		}

		private async Task EnsureClassAsync(string @class, CancellationToken ct)
		{
			// Check exists
			using var get = await _http.GetAsync("/v1/schema", ct);
			get.EnsureSuccessStatusCode();
			var json = await get.Content.ReadAsStringAsync(ct);
			using var doc = JsonDocument.Parse(json);
			var exists = doc.RootElement.GetProperty("classes").EnumerateArray().Any(x =>
				x.TryGetProperty("class", out var n) && string.Equals(n.GetString(), @class, StringComparison.OrdinalIgnoreCase));
			if (exists) return;

			var body = new
			{
				@class,
				description = $"{@class} entity for semantic search",
				vectorizer = "none",
				properties = new object[]
				{
					new { name = "name", dataType = new[] { "text" } },
					new { name = "description", dataType = new[] { "text" } },
					new { name = "category", dataType = new[] { "text" } },
					new { name = "priceInfo", dataType = new[] { "text" } },
					new { name = "priceDaily", dataType = new[] { "number" } }
				}
			};

			using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/schema/classes")
			{
				Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
			};
			using var res = await _http.SendAsync(req, ct);
			if (!res.IsSuccessStatusCode)
			{
				var detail = await res.Content.ReadAsStringAsync(ct);
				_logger.LogWarning("Failed to create Weaviate class {Class}. Status {Status}. Detail: {Detail}", @class, (int)res.StatusCode, detail);
			}
		}

		public async Task UpsertAsync(string @class, Guid id, object properties, float[]? vector = null, CancellationToken ct = default)
		{
			// Endpoint cho update theo id
			var putPath = $"/v1/objects/{id}?class={Uri.EscapeDataString(@class)}&consistencyLevel=ALL";
			// Endpoint cho create (id nằm trong body, không nằm trên URL)
			var postPath = "/v1/objects?consistencyLevel=ALL";
			var body = new Dictionary<string, object>
			{
				{ "class", @class },
				{ "id", id },
				{ "properties", properties }
			};
			if (vector != null) body["vector"] = vector;

			// Helper to serialize body
			var json = JsonSerializer.Serialize(body, JsonOptions);

			// 1) Thử PUT (update) trước
			using var putReq = new HttpRequestMessage(HttpMethod.Put, putPath)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			using var putRes = await _http.SendAsync(putReq, ct);
			if (putRes.IsSuccessStatusCode) return;

			var putDetail = await putRes.Content.ReadAsStringAsync(ct);

			// Một số instance Weaviate trả lỗi khi update object chưa tồn tại:
			// "no object with id '...'"
			if (putDetail.Contains("no object with id", StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation("Weaviate object {Class}/{Id} not found on update, trying create (POST)...", @class, id);

				// 2) Thử POST (create) với id cố định (theo spec: POST /v1/objects, id trong body)
				using var postReq = new HttpRequestMessage(HttpMethod.Post, postPath)
				{
					Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
				using var postRes = await _http.SendAsync(postReq, ct);
				if (postRes.IsSuccessStatusCode) return;

				var postDetail = await postRes.Content.ReadAsStringAsync(ct);
				_logger.LogWarning("Weaviate create failed for {Class}/{Id}. Status {Status}. Detail: {Detail}", @class, id, (int)postRes.StatusCode, postDetail);
				throw new InvalidOperationException($"Weaviate create failed for {@class}/{id}. Status {(int)postRes.StatusCode}. Detail: {postDetail}");
			}

			// Các lỗi khác: ném exception như cũ
			_logger.LogWarning("Weaviate upsert failed for {Class}/{Id}. Status {Status}. Detail: {Detail}", @class, id, (int)putRes.StatusCode, putDetail);
			throw new InvalidOperationException($"Weaviate upsert failed for {@class}/{id}. Status {(int)putRes.StatusCode}. Detail: {putDetail}");
		}

		public async Task<WeaviateQueryResult> NearTextAsync(string @class, string query, int limit = 5, CancellationToken ct = default)
		{
			var gql = $$"""
			{
			  Get {
			    {{@class}}(
			      limit: {{limit}},
			      nearText: { concepts: ["{{EscapeGraphQl(query)}}"] }
			    ){
			      _additional { id distance }
			      name
			      description
			      category
			      priceInfo
			    }
			  }
			}
			""";

			var body = new { query = gql };
			using var res = await _http.PostAsJsonAsync("/v1/graphql", body, JsonOptions, ct);
			var text = await res.Content.ReadAsStringAsync(ct);
			res.EnsureSuccessStatusCode();
			return JsonSerializer.Deserialize<WeaviateQueryResult>(text, JsonOptions) ?? new WeaviateQueryResult();
		}

		public async Task<WeaviateQueryResult> NearVectorAsync(string @class, float[] vector, int limit = 5, CancellationToken ct = default)
		{
			var vectorString = string.Join(",", vector.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
			var gql = $$"""
			{
			  Get {
			    {{@class}}(
			      limit: {{limit}},
			      nearVector: { vector: [{{vectorString}}] }
			    ){
			      {{GraphQlFragments.DefaultFields}}
			    }
			  }
			}
			""";
			var body = new { query = gql };
			using var res = await _http.PostAsJsonAsync("/v1/graphql", body, JsonOptions, ct);
			var text = await res.Content.ReadAsStringAsync(ct);
			res.EnsureSuccessStatusCode();
			return JsonSerializer.Deserialize<WeaviateQueryResult>(text, JsonOptions) ?? new WeaviateQueryResult();
		}

		public async Task<WeaviateQueryResult> HybridAsync(string @class, string query, float[]? vector, int limit = 5, float alpha = 0.5f, CancellationToken ct = default)
		{
			var vecPart = vector is { Length: > 0 }
				? $"vector: [{string.Join(",", vector.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)))}],"
				: "";
			var gql = $$"""
			{
			  Get {
			    {{@class}}(
			      limit: {{limit}},
			      hybrid: { query: "{{EscapeGraphQl(query)}}", {{vecPart}} alpha: {{alpha.ToString(System.Globalization.CultureInfo.InvariantCulture)}} }
			    ){
			      {{GraphQlFragments.DefaultFields}}
			    }
			  }
			}
			""";
			var body = new { query = gql };
			using var res = await _http.PostAsJsonAsync("/v1/graphql", body, JsonOptions, ct);
			var text = await res.Content.ReadAsStringAsync(ct);
			res.EnsureSuccessStatusCode();
			return JsonSerializer.Deserialize<WeaviateQueryResult>(text, JsonOptions) ?? new WeaviateQueryResult();
		}

		private static string EscapeGraphQl(string input) => input.Replace("\\", "\\\\").Replace("\"", "\\\"");

		public async Task DeleteAsync(string @class, Guid id, CancellationToken ct = default)
		{
			var path = $"/v1/objects/{id}?class={Uri.EscapeDataString(@class)}";
			using var res = await _http.DeleteAsync(path, ct);
			// if not exists -> ignore
			if (!res.IsSuccessStatusCode && res.StatusCode != System.Net.HttpStatusCode.NotFound)
			{
				var detail = await res.Content.ReadAsStringAsync(ct);
				_logger.LogWarning("Weaviate delete failed for {Class}/{Id}. Status {Status}. Detail: {Detail}", @class, id, (int)res.StatusCode, detail);
			}
		}
	}

	public static class GraphQlFragments
	{
		public const string DefaultFields = @"
			      _additional { id distance }
			      name
			      description
			      category
			      priceInfo";
	}

	public sealed class WeaviateQueryResult
	{
		public WeaviateData? Data { get; set; }
	}
	public sealed class WeaviateData
	{
		public WeaviateClassResults? Get { get; set; }
	}
	public sealed class WeaviateClassResults
	{
		public List<WeaviateObject> Camera { get; set; } = new();
		public List<WeaviateObject> Accessory { get; set; } = new();
		public List<WeaviateObject> Combo { get; set; } = new();
	}
	public sealed class WeaviateObject
	{
		public string? Name { get; set; }
		public string? Description { get; set; }
		public string? Category { get; set; }
		public string? PriceInfo { get; set; }
		public WeaviateAdditional _Additional { get; set; } = new();
	}
	public sealed class WeaviateAdditional
	{
		public string Id { get; set; } = string.Empty;
		public float? Distance { get; set; }
	}
}


