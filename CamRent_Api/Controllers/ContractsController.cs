using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
		[SwaggerOperation(Summary = "Tạo instance hợp đồng từ booking", Description = "Sinh một bản hợp đồng dựa trên booking và template cụ thể. Quyền: BranchManager/Admin")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateContractRequest request)
		{
			var id = await _contractService.CreateInstanceAsync(request.BookingId, request.TemplateId);
			return Ok(id);
		}

		[HttpPost("{id:guid}/sign")]
		[Authorize]
		[SwaggerOperation(Summary = "Đánh dấu hợp đồng đã ký", Description = "Cập nhật trạng thái hợp đồng sang Signed kèm URL file chữ ký thủ công. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> Sign(Guid id, [FromBody] SignContractRequest request)
		{
			await _contractService.MarkSignedAsync(id, request.SignedFileUrl);
			return NoContent();
		}

		[HttpGet("preview/{bookingId:guid}")]
		[Authorize]
		[SwaggerOperation(Summary = "Xem trước hợp đồng", Description = "Render file PDF preview của hợp đồng dựa trên bookingId để người dùng xem trước. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> Preview(Guid bookingId)
		{
			var pdf = await _templateService.GeneratePreviewPdfAsync(bookingId, HttpContext.RequestAborted);
			return File(pdf, "application/pdf", $"Contract_{bookingId}.pdf");
		}

		[HttpPost("{id:guid}/init-sign")]
		[Authorize]
		[SwaggerOperation(Summary = "Khởi tạo luồng ký điện tử", Description = "Tạo envelope ký trên provider tích hợp và trả về URL để renter ký số. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<string>> InitSign(Guid id)
		{
			var url = await _signatureProvider.CreateEnvelopeAsync(id, HttpContext.RequestAborted);
			return Ok(new { signUrl = url });
		}

		[HttpPost("webhook")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Webhook nhà cung cấp eSign", Description = "Nhận callback từ provider ký điện tử (nếu có), cập nhật trạng thái hợp đồng. Quyền: Công khai (internal)")]
		public async Task<IActionResult> Webhook([FromBody] string payload, [FromHeader(Name = "X-Signature")] string? sig)
		{
			await _signatureProvider.HandleWebhookAsync(payload, sig, HttpContext.RequestAborted);
			return Ok();
		}
	}
}
