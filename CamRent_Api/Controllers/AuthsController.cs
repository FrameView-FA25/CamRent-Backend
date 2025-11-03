using CamRent_Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.AuthModel;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthsController : ControllerBase
	{
		private readonly IUserService _userService;
		public AuthsController(IUserService userService)
		{
			_userService = userService;
		}

		//[HttpPost("login")]
		//public async Task<IActionResult> Login([FromBody] LoginRequest request)
		//{

		//}

		//[HttpPost("register")]
		//public async Task<IActionResult> RegisterAsRenter([FromBody] RegisterRequest request)
		//{

		//}
	}
}
