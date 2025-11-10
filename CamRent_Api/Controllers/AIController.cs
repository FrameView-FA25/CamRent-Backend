using CamRent_Application.IServices;
using CamRent_Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AIController : ControllerBase
	{
		private readonly IAIRecommendationService _ai;

		public AIController(IAIRecommendationService ai)
		{
			_ai = ai;
		}

		public sealed class RecommendRequest
		{
			public string Query { get; set; } = string.Empty;
			public int TopK { get; set; } = 5;
		}

		[HttpPost("recommend")]
		public async Task<ActionResult<IReadOnlyList<VectorSearchResult>>> Recommend([FromBody] RecommendRequest req, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(req.Query)) return BadRequest("Query is required");
			var results = await _ai.RecommendAsync(req.Query, Math.Clamp(req.TopK, 1, 20), ct);
			return Ok(results);
		}

		[HttpPost("reindex")]
		public async Task<IActionResult> Reindex(CancellationToken ct)
		{
			await _ai.ReindexAllAsync(ct);
			return Ok(new { ok = true });
		}
	}
}

 