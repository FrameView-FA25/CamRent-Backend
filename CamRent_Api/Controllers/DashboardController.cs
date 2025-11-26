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
	}
}




