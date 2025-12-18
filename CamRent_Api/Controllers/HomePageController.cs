using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Application.DTOs.HomePageDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class HomePageController : ControllerBase
	{
		private readonly IHomePageService _home;

		public HomePageController(IHomePageService home)
		{
			_home = home;
		}

		[HttpGet("carousel")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Carousel trang home", Description = "Danh sách slide carousel (mặc định chỉ active).")]
		public async Task<IActionResult> GetCarousel([FromQuery] bool includeInactive = false, CancellationToken ct = default)
		{
			var items = await _home.GetCarouselAsync(includeInactive, ct);
			return Ok(items);
		}

		public sealed class CreateCarouselRequest
		{
			public string Title { get; set; } = string.Empty;
			public string Content { get; set; } = string.Empty;
			public string? LinkUrl { get; set; }
			public int SortOrder { get; set; } = 0;
			public bool IsActive { get; set; } = true;
			public IFormFile Image { get; set; } = default!;
		}

		[HttpPost("carousel")]
		[Authorize(Policy = "AdminOnly")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Tạo slide carousel", Description = "Admin upload ảnh + title/content cho carousel trang home.")]
		public async Task<IActionResult> CreateCarousel([FromForm] CreateCarouselRequest req, CancellationToken ct)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			if (req.Image == null || req.Image.Length == 0)
				return BadRequest("Image is required");

			var id = await _home.CreateCarouselItemAsync(
				req.Title,
				req.Content,
				req.LinkUrl,
				req.SortOrder,
				req.IsActive,
				req.Image,
				Guid.Parse(userIdStr),
				ct);

			return Ok(new { id });
		}

		public sealed class UpdateCarouselRequest
		{
			public string? Title { get; set; }
			public string? Content { get; set; }
			public string? LinkUrl { get; set; }
			public int? SortOrder { get; set; }
			public bool? IsActive { get; set; }
			public IFormFile? Image { get; set; }
		}

		[HttpPut("carousel/{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Cập nhật slide carousel", Description = "Admin sửa title/content, bật/tắt, đổi ảnh.")]
		public async Task<IActionResult> UpdateCarousel(Guid id, [FromForm] UpdateCarouselRequest req, CancellationToken ct)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			var ok = await _home.UpdateCarouselItemAsync(
				id,
				req.Title,
				req.Content,
				req.LinkUrl,
				req.SortOrder,
				req.IsActive,
				req.Image,
				Guid.Parse(userIdStr),
				ct);

			if (!ok) return NotFound();
			return NoContent();
		}

		[HttpPut("carousel/reorder")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Reorder carousel", Description = "Admin gửi list {id, sortOrder} để sắp xếp lại thứ tự.")]
		public async Task<IActionResult> ReorderCarousel([FromBody] List<ReorderCarouselItemRequest> items, CancellationToken ct)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			var ok = await _home.ReorderCarouselAsync(items, Guid.Parse(userIdStr), ct);
			if (!ok) return BadRequest();
			return NoContent();
		}

		[HttpDelete("carousel/{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Xóa slide carousel", Description = "Xóa slide và xóa luôn ảnh Cloudinary (nếu có).")]
		public async Task<IActionResult> DeleteCarousel(Guid id, CancellationToken ct)
		{
			var ok = await _home.DeleteCarouselItemAsync(id, ct);
			if (!ok) return NotFound();
			return NoContent();
		}

		// =========================
		// HomePage Blocks (Title/Content/Image)
		// =========================

		[HttpGet("blocks")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Blocks trang home", Description = "Danh sách blocks (mặc định chỉ active) để FE render section như hero/testimonials.")]
		public async Task<IActionResult> GetBlocks([FromQuery] bool includeInactive = false, CancellationToken ct = default)
		{
			// Chỉ Admin mới được xem inactive
			var isAdmin = User?.Claims?.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin") ?? false;
			if (includeInactive && !isAdmin) includeInactive = false;

			var items = await _home.GetBlocksAsync(includeInactive, ct);
			return Ok(items);
		}

		[HttpGet("blocks/{key}")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Chi tiết block theo key", Description = "Lấy 1 block theo key (mặc định chỉ active).")]
		public async Task<IActionResult> GetBlockByKey([FromRoute] string key, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
		{
			var isAdmin = User?.Claims?.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin") ?? false;
			if (includeInactive && !isAdmin) includeInactive = false;

			var item = await _home.GetBlockByKeyAsync(key, includeInactive, ct);
			if (item == null) return NotFound();
			return Ok(item);
		}

		public sealed class UpsertBlockRequest
		{
			public string Title { get; set; } = string.Empty;
			public string Content { get; set; } = string.Empty;
			public int SortOrder { get; set; } = 0;
			public bool IsActive { get; set; } = true;
			public IFormFile? Image { get; set; }
		}

		[HttpPut("blocks/{key}")]
		[Authorize(Policy = "AdminOnly")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Upsert block", Description = "Admin tạo/cập nhật block theo key: title/content/sortOrder/isActive và ảnh (optional).")]
		public async Task<IActionResult> UpsertBlock([FromRoute] string key, [FromForm] UpsertBlockRequest req, CancellationToken ct)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			try
			{
				var id = await _home.UpsertBlockAsync(
					key,
					req.Title,
					req.Content,
					req.SortOrder,
					req.IsActive,
					req.Image,
					Guid.Parse(userIdStr),
					ct);

				return Ok(new { id });
			}
			catch (CamRent_Application.Common.AppException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpDelete("blocks/{key}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Xóa block", Description = "Xóa block theo key và xóa luôn ảnh (nếu có).")]
		public async Task<IActionResult> DeleteBlock([FromRoute] string key, CancellationToken ct)
		{
			var ok = await _home.DeleteBlockAsync(key, ct);
			if (!ok) return NotFound();
			return NoContent();
		}

		// =========================
		// Feedback (Homepage testimonials)
		// =========================

		[HttpGet("feedback")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Feedback trang home", Description = "Danh sách review đã duyệt (Approved) để hiển thị ở homepage.")]
		public async Task<IActionResult> GetFeedback([FromQuery] int limit = 3, CancellationToken ct = default)
		{
			var items = await _home.GetFeedbackAsync(limit, ct);
			return Ok(items);
		}
	}
}

