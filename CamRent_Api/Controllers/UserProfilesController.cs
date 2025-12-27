using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
		[SwaggerOperation(Summary = "Thông tin hồ sơ người dùng hiện tại", Description = "Trả về profile của người dùng dựa trên token (NameIdentifier), bao gồm thông tin chi nhánh nếu user là Staff hoặc BranchManager. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<object>> GetUserProfile()
		{
			// Dùng thông tin trong token để lấy đúng hồ sơ (User) của người đang đăng nhập.
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var p = await _userService.GetUserProfileWithBranchAsync(Guid.Parse(userId));
			return Ok(p);
		}

		[HttpPut("{userId:guid}")]
		[SwaggerOperation(Summary = "Cập nhật thông tin ngân hàng của user", Description = "Cập nhật thông tin tài khoản ngân hàng cho userId cung cấp. Thực tế FE nên truyền đúng ID của chính user hiện tại. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> UpdateUserBank(Guid userId, [FromBody] UpdateProfileRequest req)
		{
			// Chỉ cho phép user tự cập nhật thông tin ngân hàng của chính mình
			var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
							  ?? User.FindFirst("sub")?.Value
							  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(currentUserId) || Guid.Parse(currentUserId) != userId)
				return Forbid();

			await _userService.UpdateProfileAsync(Guid.Parse(currentUserId), req.BankNo, req.BankName, req.BankAccName);
			return NoContent();
		}

		[HttpPut("me")]
		[SwaggerOperation(
			Summary = "Cập nhật thông tin tài khoản của chính người dùng",
			Description = "Người dùng đã đăng nhập tự cập nhật email, họ tên, số điện thoại và địa chỉ của chính mình. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateAccountRequest req)
		{
			// Cho phép user tự sửa email / tên / điện thoại / địa chỉ của chính mình (self‑service profile).
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			await _userService.UpdateAccountAsync(
				Guid.Parse(userId),
				req.Email,
				req.FullName,
				req.Phone,
				req.Address);

			return NoContent();
		}
		[HttpPut("sign")]
		[Authorize(Policy = ("BranchManager"))] // bất kỳ user đăng nhập
		[SwaggerOperation(
			Summary = "Cập nhật chữ kí",
			Description = "Cập nhật chữ ký cho user hiện tại. Quyền: Người dùng đã đăng nhập"
		)]
		public async Task<IActionResult> UpdateUserSign([FromBody] UpdateSignRequest req)
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
							 ?? User.FindFirst("sub")
							 ?? User.FindFirst("uid");

			if (userIdClaim == null)
				return Unauthorized();

			var userId = Guid.Parse(userIdClaim.Value);

			var result = await _userService.UpdateUserSignAsync(userId, req.SignatureBase64);
			if(result <= 0)
			{
				return BadRequest("Cập nhật chữ ký thất bại.");
			}
			return Ok("Cập nhật chữ ký thành công");
		}

		public sealed class UpdateAvatarRequest
		{
			public IFormFile Avatar { get; set; } = default!;
		}

		[HttpPut("me/avatar")]
		[Authorize] // mọi user đăng nhập
		[Consumes("multipart/form-data")]
		[SwaggerOperation(
			Summary = "Cập nhật avatar của chính user",
			Description = "Upload avatar mới (sẽ xóa avatar cũ nếu có) và cập nhật AvatarId của user hiện tại.")]
		public async Task<IActionResult> UpdateMyAvatar([FromForm] UpdateAvatarRequest req, CancellationToken ct)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			if (req.Avatar == null || req.Avatar.Length == 0)
				return BadRequest("Avatar file is required");

			var (assetId, url) = await _userService.UpdateAvatarAsync(Guid.Parse(userId), req.Avatar, ct);
			return Ok(new { assetId, url });
		}
	}
}
