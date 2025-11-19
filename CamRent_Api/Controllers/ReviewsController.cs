using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
		[SwaggerOperation(Summary = "Renter review camera", Description = "Tạo đánh giá cho một camera sau khi sử dụng, bao gồm rating và nội dung. Quyền: Renter")]
		public async Task<ActionResult<Guid>> CreateForCamera([FromBody] CreateCameraReviewRequest request)
		{
			var id = await _reviewService.CreateForCameraAsync(request.AuthorUserId, request.TargetCameraId, request.Rating, request.Content);
			return Ok(id);
		}

		[HttpPost("accessory")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Renter review phụ kiện", Description = "Tạo đánh giá cho accessory sau khi sử dụng. Quyền: Renter")]
		public async Task<ActionResult<Guid>> CreateForAccessory([FromBody] CreateAccessoryReviewRequest request)
		{
			var id = await _reviewService.CreateForAccessoryAsync(request.AuthorUserId, request.TargetAccessoryId, request.Rating, request.Content);
			return Ok(id);
		}
	}
}
