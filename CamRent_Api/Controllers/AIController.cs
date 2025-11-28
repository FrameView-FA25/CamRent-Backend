using CamRent_Application.IServices;
using CamRent_Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Logging;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AIController : ControllerBase
	{
		private readonly IAIRecommendationService _ai;
		private readonly ILogger<AIController> _logger;

		public AIController(IAIRecommendationService ai, ILogger<AIController> logger)
		{
			_ai = ai;
			_logger = logger;
		}

		public sealed class RecommendRequest
		{
			public string Query { get; set; } = string.Empty;
			public int TopK { get; set; } = 5;
		}

		[HttpPost("recommend")]
		[SwaggerOperation(
			Summary = "Gợi ý thiết bị bằng AI",
			Description = "Nhận query tự nhiên của người dùng và trả về danh sách thiết bị gợi ý dựa trên vector search.")]
		public async Task<ActionResult<IReadOnlyList<VectorSearchResult>>> Recommend([FromBody] RecommendRequest req, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(req.Query)) return BadRequest("Query is required");
			var results = await _ai.RecommendAsync(req.Query, Math.Clamp(req.TopK, 1, 20), ct);
			return Ok(results);
		}

		[HttpPost("reindex")]
		[SwaggerOperation(
			Summary = "Re-index dữ liệu AI",
			Description = "Kích hoạt việc index lại toàn bộ dữ liệu thiết bị vào vector store (tốn thời gian, chỉ dành cho admin).")]
		public async Task<IActionResult> Reindex(CancellationToken ct)
		{
			try
			{
				await _ai.ReindexAllAsync(ct);
				return Ok(new { ok = true });
			}
			catch (OperationCanceledException)
			{
				// request bị hủy
				return StatusCode(StatusCodes.Status499ClientClosedRequest, new { ok = false, error = "Request was cancelled" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ReindexAllAsync failed");
				return StatusCode(StatusCodes.Status500InternalServerError, new { ok = false, error = ex.Message });
			}
		}
	}
}

 