using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Api.Models.UserProfileModel;

namespace CamRent_Api.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class UserProfilesController : ControllerBase
	{
		private readonly IUserService _userService;
		public UserProfilesController(IUserService userService)
		{
			_userService = userService;
		}

		[HttpGet("me")]
		[SwaggerOperation(Summary = "Thông tin hồ sơ người dùng hiện tại", Description = "Trả về profile của người dùng dựa trên token (NameIdentifier). Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<object>> GetUserProfile()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var p = await _userService.GetUserProfileById(Guid.Parse(userId));
			return Ok(p);
		}

		[HttpPut("me")]
		[SwaggerOperation(Summary = "Cập nhật hồ sơ tài khoản của chính mình", Description = "Người dùng đã đăng nhập có thể cập nhật email, tên, phone, địa chỉ, KYC và thông tin ngân hàng của chính họ. Không phụ thuộc role.")]
		public async Task<IActionResult> Update([FromBody] UpdateProfileRequest req)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			var userId = Guid.Parse(userIdStr);

			await _userService.UpdateProfileAsync(
				userId,
				req.NationalId,
				req.KycStatus,
				req.BankNo,
				req.BankName,
				req.BankAccName,
				req.FullName,
				req.Phone,
				req.Email,
				req.Country,
				req.Province,
				req.District);
			return NoContent();
		}
	}
}
