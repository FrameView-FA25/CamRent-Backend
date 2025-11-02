using CamRent_Application.IServices;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.UserProfileModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UserProfilesController : ControllerBase
	{
		private readonly IUserProfileService _userProfileService;
		public UserProfilesController(IUserProfileService userProfileService)
		{
			_userProfileService = userProfileService;
		}

		[HttpGet("{userId:guid}")]
		public async Task<ActionResult<object>> Get(Guid userId)
		{
			var p = await _userProfileService.GetByUserIdAsync(userId);
			return Ok(p);
		}

		[HttpPut("{userId:guid}")]
		public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateProfileRequest req)
		{
			await _userProfileService.UpdateAsync(userId, req.NationalId, req.KycStatus, req.BankNo, req.BankName, req.BankAccName);
			return NoContent();
		}
	}
}
