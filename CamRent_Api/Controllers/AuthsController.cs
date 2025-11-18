using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.AuthModel;
using static CamRent_Application.DTOs.AuthDTO;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	
	[Route("api/[controller]")]
	[ApiController]
	public class AuthsController : ControllerBase
	{
		private readonly IAuthService _authService;
		private readonly IPasswordResetService _passwordReset;
		public AuthsController(IAuthService authService, IPasswordResetService passwordReset)
		{
			_authService = authService;
			_passwordReset = passwordReset;
		}
		[AllowAnonymous]
		[HttpPost("Login")]
		[SwaggerOperation(Summary = "Đăng nhập", Description = "Xác thực người dùng và trả về token JWT. Quyền: Công khai")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var token = await _authService.GetToken(request.Email, request.Password);
			return Ok(token);
		}
		[AllowAnonymous]
		[HttpPost("RenterRegister")]
		[SwaggerOperation(Summary = "Đăng ký người thuê (Renter)", Description = "Đăng ký tài khoản với vai trò Renter. Quyền: Công khai")]
		public async Task<IActionResult> RegisterAsRenter([FromBody] RegisterRequest request)
		{
			request.Role = UserRole.Renter;
			var result = await _authService.Register(request, null);
			if (!result)
				return BadRequest("Email đã được đăng kí.");
			return Ok("Đăng ký thành công.");
		}
		[AllowAnonymous]
		[HttpPost("OwnerRegister")]
		[SwaggerOperation(Summary = "Đăng ký chủ sở hữu (Owner)", Description = "Đăng ký tài khoản với vai trò Owner. Quyền: Công khai")]
		public async Task<IActionResult> RegisterAsOwner([FromBody] RegisterRequest request)
		{
			request.Role = UserRole.Owner;
			var result = await _authService.Register(request, null);
			if (!result)
				return BadRequest("Email đã được đăng kí.");
			return Ok("Đăng ký thành công.");
		}

		[AllowAnonymous]
		[HttpPost("forgot-password")]
		[SwaggerOperation(Summary = "Yêu cầu đặt lại mật khẩu", Description = "Gửi email yêu cầu đặt lại mật khẩu. Luôn trả 200 để tránh dò tìm người dùng. Quyền: Công khai")]
		public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
		{
			await _passwordReset.RequestResetAsync(req.Email, req.ContinueUrl, HttpContext.RequestAborted);
			return Ok(new { ok = true });
		}

		[AllowAnonymous]
		[HttpPost("reset-password")]
		[SwaggerOperation(Summary = "Đặt lại mật khẩu", Description = "Đặt lại mật khẩu bằng token nhận được qua email. Quyền: Công khai")]
		public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
		{
			var ok = await _passwordReset.ResetAsync(req.Email, req.Token, req.NewPassword, HttpContext.RequestAborted);
			if (!ok) return BadRequest(new { ok = false });
			return Ok(new { ok = true });
		}
	}
}
