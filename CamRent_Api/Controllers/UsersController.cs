using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Policy = "AdminOnly")]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;

		public UsersController(IUserService userService)
		{
			_userService = userService;
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
	}
}


