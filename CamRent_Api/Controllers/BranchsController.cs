using CamRent_Application.IServices;
using CamRent_Application.Services;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
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
		[SwaggerOperation(Summary = "Lấy thành viên chi nhánh", Description = "Trả về danh sách membership của chi nhánh. Nếu người gọi là BranchManager sẽ trả kết quả theo manager. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> GetBranchMemberships(Guid? branchId)
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
			Guid? managerId = Guid.Parse(userId!);
			var memberships = await _branchService.GetBranchMembershipsAsync(branchId, managerId);
			return Ok(memberships);
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
			request.Role = UserRole.BranchManager;
			var verify = await _authService.Register(request, Guid.Parse(userId));
			if (!verify)
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
			request.Role = UserRole.Staff;
			var verify = await _authService.Register(request, Guid.Parse(userId));
			if (!verify)
				return BadRequest("Email đã được đăng kí.");

			var staffId = await _userService.GetUserIdByManagerId(Guid.Parse(userId));
			var branchId = await _branchService.GetBranchIdByManagerIdAsync(Guid.Parse(userId));
			if (staffId != null && branchId != null)
			{
				await _branchService.AssignStaffToBranchAsync(branchId, staffId);
			}
			return Ok("Đăng ký thành công.");
		}
	}
}
