using CamRent_Application.Services.Models;

namespace CamRent_Application.Interfaces
{
	public interface IVectorStore
	{
		Task EnsureSchemaAsync(CancellationToken ct = default);
		Task UpsertAsync(VectorUpsertItem item, CancellationToken ct = default);
		Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string query, string @class, int topK = 5, CancellationToken ct = default);
	}
}


