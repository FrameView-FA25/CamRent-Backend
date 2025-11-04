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
		public PaymentsController(IPaymentService paymentService, IPricingService pricingService)
		{
			_paymentService = paymentService;
			_pricingService = pricingService;
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


