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

		[HttpGet("UserID")]
		[SwaggerOperation(Summary = "Thông tin hồ sơ người dùng hiện tại", Description = "Trả về profile của người dùng dựa trên token (NameIdentifier). Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<object>> GetUserProfile()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var p = await _userService.GetUserProfileById(Guid.Parse(userId));
			return Ok(p);
		}

		[HttpPut("{userId:guid}")]
		[SwaggerOperation(Summary = "Cập nhật hồ sơ người dùng", Description = "Cập nhật thông tin định danh/KYC và tài khoản ngân hàng của userId cung cấp. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateProfileRequest req)
		{
			await _userService.UpdateProfileAsync(userId, req.NationalId, req.KycStatus, req.BankNo, req.BankName, req.BankAccName);
			return NoContent();
		}
	}
}
