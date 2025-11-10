using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.PaymentModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class PaymentsController : ControllerBase
	{
    	private readonly IPaymentService _paymentService;
    	private readonly IPricingService _pricingService;
    	private readonly IVnPayService _vnPayService;
    	public PaymentsController(IPaymentService paymentService, IPricingService pricingService, IVnPayService vnPayService)
		{
			_paymentService = paymentService;
			_pricingService = pricingService;
			_vnPayService = vnPayService;
		}

		[HttpPost("authorize")]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<Guid>> Authorize([FromBody] CreateAuthorizationRequest request)
		{
			var quote = await _pricingService.QuoteBookingAsync(request.BookingId);
			var id = await _paymentService.CreateAuthorizationAsync(request.BookingId, quote.RentalTotal, quote.DepositTotal);
			return Ok(id);
		}

		[HttpPost("{id:guid}/lines")]
		[Authorize(Policy = "BranchManager")]
		public async Task<IActionResult> AddLine(Guid id, [FromBody] AddLineRequest request)
		{
			await _paymentService.AddLineAsync(id, request.Type, request.Amount);
			return NoContent();
		}

		[HttpPost("{id:guid}/capture")]
		[Authorize(Policy = "BranchManager")]
		public async Task<IActionResult> Capture(Guid id, [FromBody] CaptureRequest request)
		{
			await _paymentService.CaptureAsync(id, request.Amount);
			return NoContent();
		}

		[HttpPost("{id:guid}/refund")]
		[Authorize(Policy = "BranchManager")]
		public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request)
		{
			await _paymentService.RefundAsync(id, request.Amount);
			return NoContent();
		}

		// Init VNPay redirect URL
		[HttpPost("{id:guid}/vnpay")]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<string>> InitVnPay(Guid id, [FromBody] InitVietQrRequest request)
		{
			// Reuse request model: Amount, Description
			var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
			var url = await _vnPayService.CreatePaymentUrlAsync(id, request.Amount, request.Description ?? $"CamRent Payment {id}", ip, HttpContext.RequestAborted);
			return Ok(new { redirectUrl = url });
		}

		// VNPay return URL
		[HttpGet("vnpay-return")]
		[AllowAnonymous]
		public async Task<IActionResult> VnPayReturn()
		{
			var query = HttpContext.Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
			var ok = await _vnPayService.ProcessReturnAsync(query, HttpContext.RequestAborted);
			if (ok) return Ok(new { ok = true });
			return BadRequest(new { ok = false });
		}

		// Test-only: confirm capture after manual transfer verification
		[HttpPost("{id:guid}/confirm-test")]
		[Authorize(Policy = "BranchManager")]
		public async Task<IActionResult> ConfirmTest(Guid id, [FromBody] CaptureRequest request)
		{
			await _paymentService.CaptureAsync(id, request.Amount);
			return NoContent();
		}

		// Áp dụng tổng dispute vào payment như một line "dispute"
		[HttpPost("{id:guid}/apply-dispute/{disputeId:guid}")]
		[Authorize(Policy = "BranchManager")]
		public async Task<IActionResult> ApplyDispute(Guid id, Guid disputeId, [FromServices] IUnitOfWork unitOfWork)
		{
			var dispute = await unitOfWork.Repository<Dispute>().GetByIdAsync(disputeId);
			if (dispute == null) return NotFound();
			var items = await unitOfWork.Repository<DisputeItem>().ListAsync(di => di.DisputeId == disputeId);
			var total = items.Sum(i => i.Amount);
			await _paymentService.AddLineAsync(id, "dispute", total);
			return NoContent();
		}
	}
}


