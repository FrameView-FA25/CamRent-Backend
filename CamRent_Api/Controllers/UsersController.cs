using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Application.DTOs.AuthDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Policy = "AdminOnly")]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;
		private readonly IAuthService _authService;

		public UsersController(IUserService userService, IAuthService authService)
		{
			_userService = userService;
			_authService = authService;
		}

		public sealed class AdminUserResponse
		{
			public Guid Id { get; set; }
			public string Email { get; set; } = string.Empty;
			public string Phone { get; set; } = string.Empty;
			public string FullName { get; set; } = string.Empty;
			public UserStatus Status { get; set; }
			public DateTime CreatedAt { get; set; }
			public string[] Roles { get; set; } = Array.Empty<string>();
		}

		public sealed class CreateUserRequest
		{
			public string Email { get; set; } = string.Empty;
			public string Phone { get; set; } = string.Empty;
			public string Password { get; set; } = "123456"; // FE nên ép đổi sau
			public string FullName { get; set; } = string.Empty;
			public UserRole Role { get; set; } = UserRole.Staff;
		}

		public sealed class UpdateUserRequest
		{
			public string? Phone { get; set; }
			public string? FullName { get; set; }
			public UserStatus? Status { get; set; }
		}

		[HttpGet]
		[SwaggerOperation(
			Summary = "Danh sách tất cả user (Admin)",
			Description = "Trả về danh sách người dùng trong hệ thống kèm role và trạng thái. Chỉ dành cho Admin.")]
		public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
		{
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 1, 200);

			var users = await _userService.GetAllUsers();

			var ordered = users.OrderByDescending(u => u.CreatedAt);
			var total = ordered.Count();
			var paged = ordered
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(u => new AdminUserResponse
				{
					Id = u.Id,
					Email = u.Email,
					Phone = u.Phone,
					FullName = u.FullName,
					Status = u.Status,
					CreatedAt = u.CreatedAt,
					Roles = u.Roles.Select(r => r.Role.ToString()).ToArray()
				})
				.ToList();

			return Ok(new
			{
				page,
				pageSize,
				total,
				items = paged
			});
		}

		[HttpPost]
		[SwaggerOperation(Summary = "Tạo user mới (Admin)", Description = "Admin tạo user mới với 1 role (Staff/Manager/Owner/...); mật khẩu tạm, FE nên buộc đổi sau.")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateUserRequest req)
		{
			var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier)
							  ?? User.FindFirst("sub")?.Value
							  ?? User.FindFirst("uid")?.Value;

			var reg = new RegisterRequest
			{
				Email = req.Email,
				Phone = req.Phone,
				Password = req.Password,
				FullName = req.FullName
			};

			var id = await _authService.Register(reg, Guid.Parse(currentAdminId!), req.Role);
			if (id == Guid.Empty)
				return BadRequest("Email đã được đăng kí.");

			return Ok(id);
		}

		[HttpPut("{id:guid}")]
		[SwaggerOperation(Summary = "Cập nhật user (Admin)", Description = "Admin cập nhật thông tin cơ bản và trạng thái user.")]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
		{
			var user = await _userService.GetUserProfileById(id);
			if (!string.IsNullOrWhiteSpace(req.Phone)) user.Phone = req.Phone;
			if (!string.IsNullOrWhiteSpace(req.FullName)) user.FullName = req.FullName;
			if (req.Status.HasValue) user.Status = req.Status.Value;

			await _userService.UpdateUser(user);
			return NoContent();
		}
		// Nếu sau này cần soft-delete, có thể thêm endpoint mới tại đây.
	}
}


