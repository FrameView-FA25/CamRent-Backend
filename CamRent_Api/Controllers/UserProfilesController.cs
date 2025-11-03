using CamRent_Application.IServices;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.UserProfileModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UserProfilesController : ControllerBase
	{
		private readonly IUserService _userService;
		public UserProfilesController(IUserService userService)
		{
			_userService = userService;
		}

		[HttpGet("{userId:guid}")]
		public async Task<ActionResult<object>> Get(Guid userId)
		{
			var p = await _userService.GetProfileAsync(userId);
			return Ok(p);
		}

		[HttpPut("{userId:guid}")]
		public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateProfileRequest req)
		{
			await _userService.UpdateProfileAsync(userId, req.NationalId, req.KycStatus, req.BankNo, req.BankName, req.BankAccName);
			return NoContent();
		}
	}
}
