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
				vectorizer = "text2vec-openai", // Expect server-side vectorization; if not available, user can switch to 'none'
				moduleConfig = new Dictionary<string, object>
				{
					{ "text2vec-openai", new Dictionary<string, object>() }
				},
				properties = new object[]
				{
					new { name = "name", dataType = new[] { "text" } },
					new { name = "description", dataType = new[] { "text" } },
					new { name = "category", dataType = new[] { "text" } },
					new { name = "priceInfo", dataType = new[] { "text" } }
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
			var path = $"/v1/objects/{id}?class={Uri.EscapeDataString(@class)}&consistencyLevel=ALL";
			var body = new Dictionary<string, object>
			{
				{ "class", @class },
				{ "id", id },
				{ "properties", properties }
			};
			if (vector != null) body["vector"] = vector;

			using var req = new HttpRequestMessage(HttpMethod.Put, path)
			{
				Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
			};
			using var res = await _http.SendAsync(req, ct);
			if (!res.IsSuccessStatusCode)
			{
				var detail = await res.Content.ReadAsStringAsync(ct);
				_logger.LogWarning("Weaviate upsert failed for {Class}/{Id}. Status {Status}. Detail: {Detail}", @class, id, (int)res.StatusCode, detail);
			}
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

		private static string EscapeGraphQl(string input) => input.Replace("\\", "\\\\").Replace("\"", "\\\"");
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


