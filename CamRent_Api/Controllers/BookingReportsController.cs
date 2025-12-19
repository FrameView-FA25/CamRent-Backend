using CamRent_Api.Hubs;
using CamRent_Application.Common;
using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;
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
			public string Title { get; set; } = string.Empty;
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
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			try
			{
				var renterId = Guid.Parse(userIdStr);
				var created = await _reports.CreateForRenterAsync(
					renterId,
					bookingId,
					req.Title,
					req.Description,
					req.Severity,
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
		}
	}
}

