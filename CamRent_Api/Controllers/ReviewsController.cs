using CamRent_Application.IServices;
using CamRent_Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;
using static CamRent_Api.Models.ReviewModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ReviewsController : ControllerBase
	{
		private readonly IReviewService _reviewService;
		private readonly IHubContext<NotificationHub> _hub;
		public ReviewsController(IReviewService reviewService, IHubContext<NotificationHub> hub)
		{
			_reviewService = reviewService;
			_hub = hub;
		}

		[HttpPost("camera")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Renter review camera", Description = "Tạo đánh giá cho một camera sau khi sử dụng, bao gồm rating và nội dung. Quyền: Renter")]
		public async Task<ActionResult<Guid>> CreateForCamera([FromBody] CreateCameraReviewRequest request)
		{
			var id = await _reviewService.CreateForCameraAsync(request.AuthorUserId, request.TargetCameraId, request.Rating, request.Content);
			// Thông báo cho staff/admin có review mới để moderation
			await _hub.Clients.Group("role:Staff")
				.SendAsync("ReviewCreated", new { Id = id, TargetType = "Camera", request.Rating });
			await _hub.Clients.Group("role:Admin")
				.SendAsync("ReviewCreated", new { Id = id, TargetType = "Camera", request.Rating });
			return Ok(id);
		}

		[HttpPost("accessory")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Renter review phụ kiện", Description = "Tạo đánh giá cho accessory sau khi sử dụng. Quyền: Renter")]
		public async Task<ActionResult<Guid>> CreateForAccessory([FromBody] CreateAccessoryReviewRequest request)
		{
			var id = await _reviewService.CreateForAccessoryAsync(request.AuthorUserId, request.TargetAccessoryId, request.Rating, request.Content);
			await _hub.Clients.Group("role:Staff")
				.SendAsync("ReviewCreated", new { Id = id, TargetType = "Accessory", request.Rating });
			await _hub.Clients.Group("role:Admin")
				.SendAsync("ReviewCreated", new { Id = id, TargetType = "Accessory", request.Rating });
			return Ok(id);
		}
	}
}
