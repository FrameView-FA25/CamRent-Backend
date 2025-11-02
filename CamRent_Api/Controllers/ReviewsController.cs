using CamRent_Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ReviewsController : ControllerBase
	{
		private readonly IReviewService _reviewService;
		public ReviewsController(IReviewService reviewService)
		{
			_reviewService = reviewService;
		}

		public class CreateCameraReviewRequest { public Guid AuthorUserId { get; set; } public Guid TargetCameraId { get; set; } public int Rating { get; set; } public string Content { get; set; } = string.Empty; }
		[HttpPost("camera")]
		public async Task<ActionResult<Guid>> CreateForCamera([FromBody] CreateCameraReviewRequest request)
		{
			var id = await _reviewService.CreateForCameraAsync(request.AuthorUserId, request.TargetCameraId, request.Rating, request.Content);
			return Ok(id);
		}

		public class CreateAccessoryReviewRequest { public Guid AuthorUserId { get; set; } public Guid TargetAccessoryId { get; set; } public int Rating { get; set; } public string Content { get; set; } = string.Empty; }
		[HttpPost("accessory")]
		public async Task<ActionResult<Guid>> CreateForAccessory([FromBody] CreateAccessoryReviewRequest request)
		{
			var id = await _reviewService.CreateForAccessoryAsync(request.AuthorUserId, request.TargetAccessoryId, request.Rating, request.Content);
			return Ok(id);
		}
	}
}
