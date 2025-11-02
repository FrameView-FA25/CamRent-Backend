using CamRent_Application.IServices;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.ContractModel;

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

		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateContractRequest request)
		{
			var id = await _contractService.CreateInstanceAsync(request.BookingId, request.TemplateId);
			return Ok(id);
		}

		[HttpPost("{id:guid}/sign")]
		public async Task<IActionResult> Sign(Guid id, [FromBody] SignContractRequest request)
		{
			await _contractService.MarkSignedAsync(id, request.SignedFileUrl);
			return NoContent();
		}
	}
}
