using CamRent_Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.VerificationModel;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class VerificationsController : ControllerBase
	{
		private readonly IVerificationService _verificationService;
		public VerificationsController(IVerificationService verificationService)
		{
			_verificationService = verificationService;
		}

		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateVerificationRequest request)
		{
			var id = await _verificationService.CreateRequestAsync(request.TargetUserId, request.BranchId, request.Notes);
			return Ok(id);
		}
	}
}
