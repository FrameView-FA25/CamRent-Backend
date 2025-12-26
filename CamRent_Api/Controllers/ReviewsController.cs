using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using CamRent_Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Api.Models.ReviewModel;
using static CamRent_Application.DTOs.ReviewDTO;

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

		private Guid? GetUserId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			return string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);
		}

		// CREATE
		[HttpPost("camera")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Renter review camera", Description = "Tạo đánh giá cho một camera sau khi sử dụng, bao gồm rating và nội dung. Quyền: Renter")]
		public async Task<ActionResult<Guid>> CreateForCamera([FromBody] CreateCameraReviewRequest request)
		{
			var userId = GetUserId();
			if (!userId.HasValue || userId.Value != request.AuthorUserId)
				return Unauthorized();

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
			var userId = GetUserId();
			if (!userId.HasValue || userId.Value != request.AuthorUserId)
				return Unauthorized();

			var id = await _reviewService.CreateForAccessoryAsync(request.AuthorUserId, request.TargetAccessoryId, request.Rating, request.Content);
			await _hub.Clients.Group("role:Staff")
				.SendAsync("ReviewCreated", new { Id = id, TargetType = "Accessory", request.Rating });
			await _hub.Clients.Group("role:Admin")
				.SendAsync("ReviewCreated", new { Id = id, TargetType = "Accessory", request.Rating });
			return Ok(id);
		}

		// READ
		[HttpGet("{id:guid}")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Lấy chi tiết review", Description = "Lấy thông tin chi tiết của một review theo ID. Quyền: Công khai")]
		public async Task<ActionResult<ReviewResponseDTO>> GetById(Guid id)
		{
			var review = await _reviewService.GetByIdAsync(id);
			if (review == null)
				return NotFound();
			return Ok(review);
		}

		[HttpGet("camera/{cameraId:guid}")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Lấy danh sách reviews của camera", Description = "Lấy tất cả reviews đã được approve của một camera. Quyền: Công khai")]
		public async Task<ActionResult<List<ReviewResponseDTO>>> GetByCameraId(Guid cameraId, [FromQuery] bool onlyApproved = true)
		{
			var reviews = await _reviewService.GetByCameraIdAsync(cameraId, onlyApproved);
			return Ok(reviews);
		}

		[HttpGet("accessory/{accessoryId:guid}")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Lấy danh sách reviews của accessory", Description = "Lấy tất cả reviews đã được approve của một accessory. Quyền: Công khai")]
		public async Task<ActionResult<List<ReviewResponseDTO>>> GetByAccessoryId(Guid accessoryId, [FromQuery] bool onlyApproved = true)
		{
			var reviews = await _reviewService.GetByAccessoryIdAsync(accessoryId, onlyApproved);
			return Ok(reviews);
		}

		[HttpGet("author/{authorId:guid}")]
		[Authorize]
		[SwaggerOperation(Summary = "Lấy danh sách reviews của một author", Description = "Lấy tất cả reviews do một user tạo. Quyền: Đã đăng nhập")]
		public async Task<ActionResult<List<ReviewResponseDTO>>> GetByAuthorId(Guid authorId)
		{
			var userId = GetUserId();
			if (!userId.HasValue)
				return Unauthorized();

			// Chỉ cho phép xem reviews của chính mình, trừ khi là admin/staff
			if (userId.Value != authorId && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
				return Forbid();

			var reviews = await _reviewService.GetByAuthorIdAsync(authorId);
			return Ok(reviews);
		}

		[HttpGet("pending")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Lấy danh sách reviews đang chờ duyệt", Description = "Lấy tất cả reviews có status Pending để Staff/Manager duyệt. Quyền: Staff, BranchManager, Admin")]
		public async Task<ActionResult<List<ReviewResponseDTO>>> GetPendingReviews()
		{
			var reviews = await _reviewService.GetPendingReviewsAsync();
			return Ok(reviews);
		}

		// UPDATE
		[HttpPut("{id:guid}")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Cập nhật review", Description = "Renter cập nhật review của chính mình (chỉ khi status = Pending). Quyền: Renter")]
		public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewRequest request)
		{
			var userId = GetUserId();
			if (!userId.HasValue)
				return Unauthorized();

			var updateDto = new UpdateReviewRequestDTO
			{
				Rating = request.Rating,
				Content = request.Content
			};

			var result = await _reviewService.UpdateReviewAsync(id, userId.Value, updateDto);
			if (!result)
				return BadRequest(new { Message = "Không thể cập nhật review. Kiểm tra lại quyền truy cập hoặc trạng thái review." });

			return Ok(new { Message = "Cập nhật review thành công." });
		}

		[HttpPut("{id:guid}/moderate")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Staff/Manager duyệt review", Description = "Staff hoặc Manager approve/reject một review. Quyền: Staff, BranchManager, Admin")]
		public async Task<IActionResult> ModerateReview(Guid id, [FromBody] ModerateReviewRequest request)
		{
			var userId = GetUserId();
			if (!userId.HasValue)
				return Unauthorized();

			var moderateDto = new ModerateReviewRequestDTO
			{
				Status = request.Status,
				ModerationNotes = request.ModerationNotes
			};

			var result = await _reviewService.ModerateReviewAsync(id, userId.Value, moderateDto);
			if (!result)
				return BadRequest(new { Message = "Không tìm thấy review hoặc cập nhật thất bại." });

			return Ok(new { Message = "Duyệt review thành công." });
		}

		// DELETE
		[HttpDelete("{id:guid}")]
		[Authorize]
		[SwaggerOperation(Summary = "Xóa review", Description = "Renter xóa review của chính mình, hoặc Admin/Staff xóa bất kỳ review nào. Quyền: Đã đăng nhập")]
		public async Task<IActionResult> DeleteReview(Guid id)
		{
			var userId = GetUserId();
			if (!userId.HasValue)
				return Unauthorized();

			var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("BranchManager");
			var result = await _reviewService.DeleteReviewAsync(id, userId.Value, isAdminOrStaff);
			if (!result)
				return BadRequest(new { Message = "Không thể xóa review. Kiểm tra lại quyền truy cập." });

			return Ok(new { Message = "Xóa review thành công." });
		}
	}
}
