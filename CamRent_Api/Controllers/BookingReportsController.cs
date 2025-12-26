using CamRent_Api.Hubs;
using CamRent_Application.Common;
using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/Bookings/{bookingId:guid}/reports")]
	public sealed class BookingReportsController : ControllerBase
	{
		private readonly IBookingReportService _reports;
		private readonly IHubContext<NotificationHub> _hub;

		public BookingReportsController(IBookingReportService reports, IHubContext<NotificationHub> hub)
		{
			_reports = reports;
			_hub = hub;
		}

		public sealed class CreateBookingReportRequest
		{
			[Required(ErrorMessage = "Title là bắt buộc")]
			public string Title { get; set; } = string.Empty;
			[Required(ErrorMessage = "Description là bắt buộc")]
			public string Description { get; set; } = string.Empty;
			public string Severity { get; set; } = "minor";
			public List<IFormFile>? Images { get; set; }
		}

		[HttpPost]
		[Authorize(Policy = "Renter")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(
			Summary = "Renter report sự cố khi đang thuê",
			Description = "Renter gửi report cho booking đang ở trạng thái PickedUp/Overdue. Có thể đính kèm ảnh.")]
		public async Task<IActionResult> Create([FromRoute] Guid bookingId, [FromForm] CreateBookingReportRequest req, CancellationToken ct)
		{
			// Validate ModelState
			if (!ModelState.IsValid)
			{
				return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = ModelState });
			}

			// Validate required fields manually (for mobile compatibility)
			if (string.IsNullOrWhiteSpace(req?.Title))
				return BadRequest(new { message = "Title là bắt buộc" });
			if (string.IsNullOrWhiteSpace(req?.Description))
				return BadRequest(new { message = "Description là bắt buộc" });

			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });

			if (!Guid.TryParse(userIdStr, out var renterId))
				return BadRequest(new { message = "User ID không hợp lệ" });

			if (bookingId == Guid.Empty)
				return BadRequest(new { message = "BookingId không hợp lệ" });

			try
			{
				var created = await _reports.CreateForRenterAsync(
					renterId,
					bookingId,
					req.Title,
					req.Description,
					req.Severity ?? "minor",
					req.Images,
					ct);

				// Realtime notify staff/manager để xử lý
				await _hub.Clients.Group("role:Staff")
					.SendAsync("BookingReportCreatedForStaff", new { created.Id, created.BookingId, created.Title, created.Severity });
				await _hub.Clients.Group("role:BranchManager")
					.SendAsync("BookingReportCreatedForManager", new { created.Id, created.BookingId, created.Title, created.Severity });

				return Ok(created);
			}
			catch (AppException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (FormatException ex)
			{
				return BadRequest(new { message = "Định dạng dữ liệu không hợp lệ", detail = ex.Message });
			}
			catch (Exception ex)
			{
				// Log exception for debugging
				// TODO: Log error: $"Unexpected error creating booking report: {ex.Message}"
				return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo report. Vui lòng thử lại sau." });
			}
		}
	}
}

