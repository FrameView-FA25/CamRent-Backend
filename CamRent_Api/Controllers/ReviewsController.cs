using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.ReviewModel;

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

		[HttpPost("camera")]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<Guid>> CreateForCamera([FromBody] CreateCameraReviewRequest request)
		{
			var id = await _reviewService.CreateForCameraAsync(request.AuthorUserId, request.TargetCameraId, request.Rating, request.Content);
			return Ok(id);
		}

		[HttpPost("accessory")]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<Guid>> CreateForAccessory([FromBody] CreateAccessoryReviewRequest request)
		{
			var id = await _reviewService.CreateForAccessoryAsync(request.AuthorUserId, request.TargetAccessoryId, request.Rating, request.Content);
			return Ok(id);
		}
	}
}
