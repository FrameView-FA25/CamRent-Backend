using CamRent_Application.IServices;
using CamRent_Application.Services;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using CamRent_Application.Common;
using static CamRent_Application.DTOs.AuthDTO;
using static CamRent_Application.DTOs.BranchDTO;

namespace CamRent_Api.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class BranchsController : ControllerBase
	{
		private readonly IBranchService _branchService;
		private readonly IAuthService _authService;
		private readonly IUserService _userService;
		public BranchsController(IBranchService branchService, IAuthService authService, IUserService userService)
		{
			_branchService = branchService;
			_authService = authService;
			_userService = userService;
		}
		[HttpGet]
		[SwaggerOperation(Summary = "Lấy danh sách chi nhánh", Description = "Trả về danh sách chi nhánh. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> GetAllBranches()
		{
			var branches = await _branchService.GetAllBranchesAsync();
			return Ok(branches);
		}
		[HttpGet("Memberships")]
		[Authorize(Policy = "BranchManager")] // BranchManager OR Admin (the policy includes Admin)
		[SwaggerOperation(
			Summary = "Lấy thành viên chi nhánh",
			Description = "BranchManager: không cần branchId, hệ thống tự lấy theo manager. Admin: bắt buộc truyền branchId để xem thành viên. Quyền: BranchManager, Admin")]
		public async Task<IActionResult> GetBranchMemberships([FromQuery] Guid? branchId)
		{
			var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
			var isManager = roles.Contains(UserRole.BranchManager.ToString());
			var isAdmin = roles.Contains(UserRole.Admin.ToString());

			Guid? managerId = null;
			if (isManager && !branchId.HasValue)
				{
				var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
				if (string.IsNullOrEmpty(userIdStr))
					return Unauthorized();
				managerId = Guid.Parse(userIdStr);
			}

			// Admin phải truyền branchId để xem một chi nhánh cụ thể
			if (isAdmin && !branchId.HasValue && managerId is null)
				return BadRequest("branchId is required for admin");

			var memberships = await _branchService.GetBranchMembershipsAsync(branchId, managerId);
			return Ok(memberships);
		}

		[HttpGet("unassigned-staff")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(
			Summary = "Danh sách Staff chưa thuộc chi nhánh",
			Description = "Trả về các user có role Staff nhưng chưa có UserBranchMembership. Quyền: Admin")]
		public async Task<IActionResult> GetUnassignedStaff()
		{
			var users = await _branchService.GetUnassignedStaffAsync();
			return Ok(users);
		}

		[HttpGet("unassigned-managers")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(
			Summary = "Danh sách BranchManager chưa thuộc chi nhánh",
			Description = "Trả về các user có role BranchManager nhưng chưa có UserBranchMembership. Quyền: Admin")]
		public async Task<IActionResult> GetUnassignedManagers()
		{
			var users = await _branchService.GetUnassignedManagersAsync();
			return Ok(users);
		}

		[HttpDelete("{branchId:guid}/members/{userId:guid}")]
		[Authorize(Policy = "BranchManager")] // BranchManager OR Admin
		[SwaggerOperation(
			Summary = "Xoá thành viên khỏi chi nhánh",
			Description = "Xoá UserBranchMembership của user trong branch. Có kiểm tra điều kiện: booking/contract/verification/inspection/dispute trước khi xoá. Quyền: BranchManager, Admin")]
		public async Task<IActionResult> RemoveMember(Guid branchId, Guid userId, CancellationToken ct)
		{
			var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
			var isAdmin = roles.Contains(UserRole.Admin.ToString());
			var isManager = roles.Contains(UserRole.BranchManager.ToString());

			// Nếu là BranchManager (không phải admin) thì chỉ được xoá trong chi nhánh mình quản lý
			if (isManager && !isAdmin)
			{
				var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
								  ?? User.FindFirst("sub")?.Value
								  ?? User.FindFirst("uid")?.Value;
				if (string.IsNullOrEmpty(currentUserIdStr))
					return Unauthorized();

				var myBranchId = await _branchService.GetBranchIdByManagerIdAsync(Guid.Parse(currentUserIdStr));
				if (myBranchId != branchId)
					return Forbid();
			}

			try
			{
				await _branchService.RemoveMemberFromBranchAsync(branchId, userId, ct);
				return NoContent();
			}
			catch (AppException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
		[HttpGet("{id:guid}")]
		[SwaggerOperation(Summary = "Lấy chi nhánh theo id", Description = "Trả về thông tin chi nhánh theo id. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> GetBranchById(Guid id)
		{
			var branch = await _branchService.GetBranchByIdAsync(id);
			if (branch == null)
			{
				return NotFound();
			}
			return Ok(branch);
		}
		[HttpPost]
		[Authorize(Policy ="AdminOnly")]
		[SwaggerOperation(Summary = "Tạo chi nhánh", Description = "Tạo một chi nhánh mới. Quyền: Admin")]
		public async Task<IActionResult> CreateBranch([FromBody] BranchRequest branchRequest)
		{
			var result = await _branchService.CreateBranchAsync(branchRequest);
			return result > 0 ? Ok(new { Message = "Tạo chi nhánh thành công." }) : BadRequest(new { Message = "Tạo chi nhánh thất bại." });
		}

		[HttpPut("{branchId:guid}/assign-manager/{managerId:guid}")]
		[SwaggerOperation(Summary = "Gán quản lý cho chi nhánh", Description = "Gán một BranchManager cho chi nhánh. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> AssignManagerToBranch(Guid branchId, Guid managerId)
		{
			var result = await _branchService.AssignManagerToBranchAsync(branchId, managerId);
			return result > 0 ? Ok(new { Message = "Gán quản lý thành công." }) : BadRequest(new { Message = "Gán quản lý thất bại." });
		}
		[HttpPut("{branchId:guid}/assign-staff/{staffId:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Gán nhân viên cho chi nhánh", Description = "Gán một Staff cho chi nhánh. Quyền: Admin")]
		public async Task<IActionResult> AssignStaffToBranch(Guid branchId, Guid staffId)
		{
			var result = await _branchService.AssignStaffToBranchAsync(branchId, staffId);
			return result > 0 ? Ok(new { Message = "Gán nhân viên thành công." }) : BadRequest(new { Message = "Gán nhân viên thất bại." });
		}

		[HttpPost("BranchManagerRegister")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Đăng ký BranchManager", Description = "Đăng ký tài khoản với vai trò BranchManager. Quyền: Admin")]
		public async Task<IActionResult> CreateManager([FromBody] RegisterRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			
			var verify = await _authService.Register(request, Guid.Parse(userId), UserRole.BranchManager);
			if (verify == Guid.Empty)
				return BadRequest("Email đã được đăng kí.");
			return Ok("Đăng ký thành công.");
		}

		[HttpPost("StaffRegister")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Đăng ký Staff", Description = "Đăng ký tài khoản với vai trò Staff. Quyền: Manager, Admin")]
		public async Task<IActionResult> CreateStaff([FromBody] RegisterRequest request)
		{
			var userId = string.Empty;
			var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
			foreach (var role in roles)
			{
				if (role == UserRole.BranchManager.ToString())
				{
					userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
				}
			}
			var verify = await _authService.Register(request, Guid.Parse(userId), UserRole.Staff);
			if (verify == Guid.Empty)
				return BadRequest("Email đã được đăng kí.");
			var branchId = await _branchService.GetBranchIdByManagerIdAsync(Guid.Parse(userId));
			if (verify != Guid.Empty && branchId != null)
			{
				await _branchService.AssignStaffToBranchAsync(branchId, verify);
			}
			return Ok("Đăng ký thành công.");
		}
	}
}
