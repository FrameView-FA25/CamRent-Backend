using CamRent_Application.Common;
using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Policy = "ManagerOrStaff")]
	public sealed class BookingIssueReportsController : ControllerBase
	{
		private readonly IBookingReportService _reports;

		public BookingIssueReportsController(IBookingReportService reports)
		{
			_reports = reports;
		}

		[HttpGet]
		[SwaggerOperation(
			Summary = "Danh sách report thiết bị bị lỗi",
			Description = "Staff/Manager xem danh sách report trong chi nhánh của mình. Kèm thiết bị thuộc booking để biết thiết bị nào bị lỗi.")]
		public async Task<IActionResult> GetList([FromQuery] string? status = "open", [FromQuery] int limit = 50, CancellationToken ct = default)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			// Hiện tại implement scope theo Staff branch membership (Admin cũng ok nếu có membership).
			// Nếu cần scope theo BranchManager.ManagerId thì mở rộng thêm sau.
			try
			{
				var items = await _reports.GetReportsForStaffAsync(Guid.Parse(userIdStr), status, limit, ct);
				return Ok(items);
			}
			catch (AppException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpGet("{id:guid}")]
		[SwaggerOperation(Summary = "Chi tiết report", Description = "Chi tiết report + thiết bị trong booking + ảnh report (nếu có).")]
		public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken ct = default)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
						  ?? User.FindFirst("sub")?.Value
						  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			try
			{
				var item = await _reports.GetReportDetailForStaffAsync(Guid.Parse(userIdStr), id, ct);
				if (item == null) return NotFound();
				return Ok(item);
			}
			catch (AppException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}

