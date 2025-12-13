using System.Security.Claims;
using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class DashboardController : ControllerBase
	{
		private readonly IDashboardService _dashboard;

		public DashboardController(IDashboardService dashboard)
		{
			_dashboard = dashboard;
		}

		// Tổng quan toàn hệ thống cho Admin
		[HttpGet("admin")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(
			Summary = "Dashboard tổng quan cho Admin",
			Description = "Trả về số liệu tổng quan toàn hệ thống: người dùng, chi nhánh, thiết bị, booking, doanh thu, dispute, ...")]
		public async Task<IActionResult> GetAdminDashboard()
		{
			var data = await _dashboard.GetAdminDashboardAsync(HttpContext.RequestAborted);
			return Ok(data);
		}

		// Dashboard cho Branch Manager (theo chi nhánh quản lý)
		[HttpGet("manager")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Dashboard cho Branch Manager",
			Description = "Trả về số liệu theo chi nhánh mà BranchManager đang quản lý: số thiết bị, booking, doanh thu, dispute.")]
		public async Task<IActionResult> GetManagerDashboard()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var data = await _dashboard.GetManagerDashboardAsync(Guid.Parse(userId), HttpContext.RequestAborted);
			return Ok(data);
		}

		// Dashboard cho Staff: các booking/nhiệm vụ được phân công cho nhân viên
		[HttpGet("staff")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(
			Summary = "Dashboard cho Staff",
			Description = "Trả về số liệu tổng quan cho nhân viên: booking được phân công, công việc trong ngày, ...")]
		public async Task<IActionResult> GetStaffDashboard()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var data = await _dashboard.GetStaffDashboardAsync(Guid.Parse(userId), HttpContext.RequestAborted);
			return Ok(data);
		}

		// Dashboard cho Owner: tổng quan thiết bị & doanh thu của riêng owner
		[HttpGet("owner")]
		[Authorize(Policy = "Owner")]
		[SwaggerOperation(
			Summary = "Dashboard cho Owner",
			Description = "Trả về số liệu tổng quan cho chủ sở hữu: số camera/phụ kiện, số booking liên quan và doanh thu ước tính cùng danh sách thiết bị được thuê nhiều nhất.")]
		public async Task<IActionResult> GetOwnerDashboard()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var data = await _dashboard.GetOwnerDashboardAsync(Guid.Parse(userId), HttpContext.RequestAborted);
			return Ok(data);
		}

		/// <summary>
		/// Lịch làm việc chi tiết của một staff (booking pickup/return + verification) cho Manager/Staff xem.
		/// </summary>
		[HttpGet("staff-schedule")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(
			Summary = "Lịch làm việc của staff",
			Description = "Trả về các event (booking pickup/return, verification) của một staff trong khoảng from-to.")]
		public async Task<IActionResult> GetStaffSchedule([FromQuery] Guid staffId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
		{
			// Nếu là staff và không truyền staffId, dùng userId hiện tại
			var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(currentUserIdStr))
				return Unauthorized();

			var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
			var currentUserId = Guid.Parse(currentUserIdStr);

			if (staffId == Guid.Empty)
			{
				// Staff tự xem lịch của mình
				staffId = currentUserId;
			}

			// BranchManager chỉ được xem lịch staff trong chi nhánh mình quản lý (simple check bỏ qua ở đây để giữ code gọn)
			var data = await _dashboard.GetStaffScheduleAsync(staffId, from, to, HttpContext.RequestAborted);
			return Ok(data);
		}

		/// <summary>
		/// Workload của staff trong chi nhánh mà Branch Manager đang quản lý:
		/// số booking, số verification được gán, số pickup/return trong ngày.
		/// </summary>
		[HttpGet("staff-workload")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Workload staff cho Branch Manager",
			Description = "Trả về workload (booking, verification, pickup/return hôm nay) cho từng staff trong chi nhánh của manager.")]
		public async Task<IActionResult> GetStaffWorkload([FromQuery] DateTime? from, [FromQuery] DateTime? to)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			var data = await _dashboard.GetStaffWorkloadForManagerAsync(Guid.Parse(userIdStr), from, to, HttpContext.RequestAborted);
			return Ok(data);
		}

		/// <summary>
		/// Tìm staff trong chi nhánh của Branch Manager đang rảnh trong khoảng [start, end),
		/// dựa trên booking (pickup/return) và verification đã được gán.
		/// </summary>
		[HttpGet("available-staff")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Tìm staff rảnh để giao việc",
			Description = "Trả về danh sách staff trong chi nhánh của manager và cờ IsAvailable trong khoảng start–end (xét booking + verification).")]
		public async Task<IActionResult> GetAvailableStaff(
			[FromQuery] DateTime start,
			[FromQuery] DateTime end,
			[FromQuery] string type = "both")
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userIdStr))
				return Unauthorized();

			if (start >= end)
				return BadRequest("start must be earlier than end");

			var data = await _dashboard.GetAvailableStaffForManagerAsync(
				Guid.Parse(userIdStr), start, end, type, HttpContext.RequestAborted);

			return Ok(data);
		}
	}
}




