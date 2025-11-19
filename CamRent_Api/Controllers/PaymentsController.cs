using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
    	private readonly IPayOsService _payOsService;
    	public PaymentsController(IPaymentService paymentService, IPricingService pricingService, IPayOsService payOsService)
		{
			_paymentService = paymentService;
			_pricingService = pricingService;
			_payOsService = payOsService;
		}

		[HttpPost("authorize")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(
			Summary = "Khởi tạo payment nội bộ cho booking",
			Description = "Tính lại quote cho booking, tạo bản ghi payment với rental + deposit và trả về paymentId để tiếp tục các bước thanh toán.")]
		public async Task<ActionResult<Guid>> Authorize([FromBody] CreateAuthorizationRequest request)
		{
			var quote = await _pricingService.QuoteBookingAsync(request.BookingId);
			var id = await _paymentService.CreateAuthorizationAsync(request.BookingId, quote.RentalTotal, quote.DepositTotal);
			return Ok(id);
		}

		[HttpPost("{id:guid}/lines")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Thêm line (rental/deposit/dispute) vào payment",
			Description = "Branch manager bổ sung một payment line mới cho paymentId tương ứng, dùng để cộng thêm khoản thu/payout.")]
		public async Task<IActionResult> AddLine(Guid id, [FromBody] AddLineRequest request)
		{
			await _paymentService.AddLineAsync(id, request.Type, request.Amount);
			return NoContent();
		}

		[HttpPost("{id:guid}/capture")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Capture thủ công một payment",
			Description = "Xác nhận đã nhận đủ tiền (ví dụ kiểm tra chuyển khoản) và chuyển payment sang trạng thái Captured, đồng thời kích hoạt quy trình sinh hợp đồng.")]
		public async Task<IActionResult> Capture(Guid id, [FromBody] CaptureRequest request)
		{
			await _paymentService.CaptureAsync(id, request.Amount);
			return NoContent();
		}

		[HttpPost("{id:guid}/refund")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Refund một phần/toàn bộ payment",
			Description = "Thực hiện hoàn tiền thủ công cho renter, cập nhật số tiền đã refund trong payment.")]
		public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request)
		{
			await _paymentService.RefundAsync(id, request.Amount);
			return NoContent();
		}

		// Init PayOS payment link
		[HttpPost("{id:guid}/payos")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(
			Summary = "Tạo link thanh toán PayOS cho payment",
			Description = "Sinh checkoutUrl PayOS cho paymentId, dùng số tiền và description truyền vào; trả về redirectUrl để FE mở trang thanh toán.")]
		public async Task<ActionResult<string>> InitPayOs(Guid id, [FromBody] InitPayOsRequest request)
		{
			var url = await _payOsService.CreatePaymentLinkAsync(id, request.Amount, request.Description ?? $"CamRent Payment {id}", request.ReturnUrl, request.CancelUrl, HttpContext.RequestAborted);
			return Ok(new { redirectUrl = url });
		}

		// Test-only: confirm capture after manual transfer verification
		[HttpPost("{id:guid}/confirm-test")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "[Test] Xác nhận payment đã receive tiền",
			Description = "Endpoint test/manual cho phép capture payment mà không cần đi qua PayOS.")]
		public async Task<IActionResult> ConfirmTest(Guid id, [FromBody] CaptureRequest request)
		{
			await _paymentService.CaptureAsync(id, request.Amount);
			return NoContent();
		}

		// Áp dụng tổng dispute vào payment như một line "dispute"
		[HttpPost("{id:guid}/apply-dispute/{disputeId:guid}")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Cộng tổng dispute vào payment",
			Description = "Tính tổng tiền tranh chấp của disputeId và thêm thành 1 payment line 'dispute' vào paymentId tương ứng.")]
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


