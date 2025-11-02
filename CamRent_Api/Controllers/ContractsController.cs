using CamRent_Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ContractsController : ControllerBase
	{
		private readonly IContractService _contractService;
		public ContractsController(IContractService contractService)
		{
			_contractService = contractService;
		}

		public class CreateContractRequest { public Guid BookingId { get; set; } public Guid TemplateId { get; set; } }
		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateContractRequest request)
		{
			var id = await _contractService.CreateInstanceAsync(request.BookingId, request.TemplateId);
			return Ok(id);
		}

		public class SignContractRequest { public string? SignedFileUrl { get; set; } }
		[HttpPost("{id:guid}/sign")]
		public async Task<IActionResult> Sign(Guid id, [FromBody] SignContractRequest request)
		{
			await _contractService.MarkSignedAsync(id, request.SignedFileUrl);
			return NoContent();
		}
	}
}
