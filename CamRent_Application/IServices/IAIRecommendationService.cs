using CamRent_Application.DTOs;

namespace CamRent_Application.IServices
{
	public interface IAIRecommendationService
	{
		Task ReindexAllAsync(CancellationToken ct = default);
		Task<IReadOnlyList<VectorSearchResult>> RecommendAsync(string query, int topK = 5, CancellationToken ct = default);
	}
}


