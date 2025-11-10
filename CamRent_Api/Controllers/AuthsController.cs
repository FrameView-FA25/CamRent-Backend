using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.AuthModel;
using static CamRent_Application.DTOs.AuthDTO;

namespace CamRent_Api.Controllers
{
	
	[Route("api/[controller]")]
	[ApiController]
	public class AuthsController : ControllerBase
	{
		private readonly IAuthService _authService;
		public AuthsController(IAuthService authService)
		{
			_authService = authService;
		}
		[AllowAnonymous]
		[HttpPost("Login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var token = await _authService.GetToken(request.Email, request.Password);
			return Ok(token);
		}

		[HttpPost("RenterRegister")]
		public async Task<IActionResult> RegisterAsRenter([FromBody] RegisterRequest request)
		{
			request.Role = UserRole.Renter;
			var result = await _authService.Register(request);
			if (!result)
				return BadRequest("Email đã được đăng kí.");
			return Ok("Đăng ký thành công.");
		}

		[HttpPost("OwnerRegister")]
		public async Task<IActionResult> RegisterAsOwner([FromBody] RegisterRequest request)
		{
			request.Role = UserRole.Owner;
			var result = await _authService.Register(request);
			if (!result)
				return BadRequest("Email đã được đăng kí.");
			return Ok("Đăng ký thành công.");
		}

		[HttpPost("BranchManagerRegister")]
		public async Task<IActionResult> RegisterAsManager([FromBody] RegisterRequest request)
		{
			request.Role = UserRole.BranchManager;
			var result = await _authService.Register(request);
			if (!result)
				return BadRequest("Email đã được đăng kí.");
			return Ok("Đăng ký thành công.");
		}
		[HttpPost("StaffRegister")]
		public async Task<IActionResult> RegisterAsStaff([FromBody] RegisterRequest request)
		{
			request.Role = UserRole.Staff;
			var result = await _authService.Register(request);
			if (!result)
				return BadRequest("Email đã được đăng kí.");
			return Ok("Đăng ký thành công.");
		}
	}
}
