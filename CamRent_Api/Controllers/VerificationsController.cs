using CamRent_Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

		public class CreateVerificationRequest { public Guid? TargetUserId { get; set; } public Guid? BranchId { get; set; } public string? Notes { get; set; } }
		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateVerificationRequest request)
		{
			var id = await _verificationService.CreateRequestAsync(request.TargetUserId, request.BranchId, request.Notes);
			return Ok(id);
		}
	}
}
