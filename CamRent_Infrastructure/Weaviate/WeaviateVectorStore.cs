using CamRent_Application.Interfaces;
using CamRent_Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CamRent_Infrastructure.Weaviate
{
	public sealed class WeaviateVectorStore : IVectorStore
	{
		private readonly IWeaviateClient _client;
		private readonly CamRent_Application.IServices.IEmbeddingService _embed;
		private readonly ILogger<WeaviateVectorStore> _logger;

		public WeaviateVectorStore(IWeaviateClient client, CamRent_Application.IServices.IEmbeddingService embed, ILogger<WeaviateVectorStore> logger)
		{
			_client = client;
			_embed = embed;
			_logger = logger;
		}

		public Task EnsureSchemaAsync(CancellationToken ct = default) => _client.EnsureSchemaAsync(ct);

		public Task UpsertAsync(VectorUpsertItem item, CancellationToken ct = default)
			=> _client.UpsertAsync(item.Class, item.Id, item.Properties, item.Vector, ct);

		public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string query, string @class, int topK = 5, CancellationToken ct = default)
		{
			// Client-side embedding for query
			var qvec = await _embed.EmbedAsync(query, ct);
			var res = await _client.NearVectorAsync(@class, qvec, topK, ct);
			var list = new List<VectorSearchResult>();
			var results = @class switch
			{
				"Camera" => res.Data?.Get?.Camera?.Select(x => (x, "Camera")),
				"Accessory" => res.Data?.Get?.Accessory?.Select(x => (x, "Accessory")),
				"Combo" => res.Data?.Get?.Combo?.Select(x => (x, "Combo")),
				_ => Enumerable.Empty<(WeaviateObject x, string c)>()
			};

			foreach (var (x, c) in results!)
			{
				if (Guid.TryParse(x._Additional.Id, out var id))
				{
					list.Add(new VectorSearchResult
					{
						Id = id,
						Class = c,
						Name = x.Name,
						Description = x.Description,
						Score = x._Additional.Distance.HasValue ? 1 - x._Additional.Distance.Value : null
					});
				}
			}
			return list;
		}
	}
}


