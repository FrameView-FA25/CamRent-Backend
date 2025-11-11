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
    	private readonly IContractSignatureProvider _signatureProvider;
    	public ContractsController(IContractService contractService, IContractTemplateService templateService, IContractSignatureProvider signatureProvider)
		{
			_contractService = contractService;
			_templateService = templateService;
			_signatureProvider = signatureProvider;
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

		[HttpPost("{id:guid}/init-sign")]
		[Authorize]
		public async Task<ActionResult<string>> InitSign(Guid id)
		{
			var url = await _signatureProvider.CreateEnvelopeAsync(id, HttpContext.RequestAborted);
			return Ok(new { signUrl = url });
		}

		[HttpPost("webhook")]
		[AllowAnonymous]
		public async Task<IActionResult> Webhook([FromBody] string payload, [FromHeader(Name = "X-Signature")] string? sig)
		{
			await _signatureProvider.HandleWebhookAsync(payload, sig, HttpContext.RequestAborted);
			return Ok();
		}
	}
}
