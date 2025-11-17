using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CamRent_Application.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CamRent_Infrastructure.Embeddings
{
	public sealed class GeminiEmbeddingService : IEmbeddingService
	{
		private readonly HttpClient _http;
		private readonly GeminiOptions _opts;
		private readonly ILogger<GeminiEmbeddingService> _logger;
		private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

		public GeminiEmbeddingService(HttpClient http, IOptions<GeminiOptions> opts, ILogger<GeminiEmbeddingService> logger)
		{
			_http = http;
			_opts = opts.Value;
			_logger = logger;
		}

		public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
		{
			var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_opts.Model}:embedContent?key={_opts.ApiKey}";
			var body = new
			{
				model = _opts.Model,
				content = new
				{
					parts = new[] { new { text } }
				}
			};
			using var res = await _http.PostAsJsonAsync(url, body, JsonOpts, ct);
			var json = await res.Content.ReadAsStringAsync(ct);
			if (!res.IsSuccessStatusCode)
			{
				_logger.LogWarning("Gemini embedding failed: {Status} {Body}", (int)res.StatusCode, json);
				throw new InvalidOperationException("Embedding failed");
			}

			var doc = JsonDocument.Parse(json);
			var vec = doc.RootElement.GetProperty("embedding").GetProperty("values").EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
			return vec;
		}
	}
}

