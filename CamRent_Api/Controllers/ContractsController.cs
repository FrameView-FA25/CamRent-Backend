using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.ContractModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ContractsController : ControllerBase
	{
    	private readonly IContractService _contractService;
    	private readonly IContractTemplateService _templateService;
    	public ContractsController(IContractService contractService, IContractTemplateService templateService)
		{
			_contractService = contractService;
			_templateService = templateService;
		}

		[HttpPost]
		[Authorize(Policy = "BranchManager")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateContractRequest request)
		{
			var id = await _contractService.CreateInstanceAsync(request.BookingId, request.TemplateId);
			return Ok(id);
		}

		[HttpPost("{id:guid}/sign")]
		[Authorize]
		public async Task<IActionResult> Sign(Guid id, [FromBody] SignContractRequest request)
		{
			await _contractService.MarkSignedAsync(id, request.SignedFileUrl);
			return NoContent();
		}

		[HttpGet("preview/{bookingId:guid}")]
		[Authorize]
		public async Task<IActionResult> Preview(Guid bookingId)
		{
			var pdf = await _templateService.GeneratePreviewPdfAsync(bookingId, HttpContext.RequestAborted);
			return File(pdf, "application/pdf", $"Contract_{bookingId}.pdf");
		}
	}
}
