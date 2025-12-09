using CamRent_Api.Hubs;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using CamRent_Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;
using static CamRent_Api.Models.PaymentModel;
using static CamRent_Application.DTOs.WalletDTO;

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
		private readonly IBookingService _bookingService;
		private readonly IWalletService _walletService;
		private readonly IHubContext<NotificationHub> _hub;

		public PaymentsController(
			IPaymentService paymentService,
			IPricingService pricingService,
			IPayOsService payOsService,
			IBookingService bookingService,
			IWalletService walletService,
			IHubContext<NotificationHub> hub)
		{
			_paymentService = paymentService;
			_pricingService = pricingService;
			_payOsService = payOsService;
			_bookingService = bookingService;
			_walletService = walletService;
			_hub = hub;
		}

		[HttpPost("authorize")]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<Guid>> Authorize([FromBody] CreateAuthorizationRequest request)
		{
			var booking = await _bookingService.GetByIdAsync(request.BookingId);
			if (booking == null) return NotFound("Booking not found");

			var renterId = booking.RenterId ?? throw new InvalidOperationException("Booking has no renter");

			var rentalTotal = booking.SnapshotRentalTotal;      // A
			var deviceDeposit = booking.SnapshotDepositAmount;  // C
			var advance = rentalTotal * booking.SnapshotPlatformFeePercent;                  // B
			var remainingRental = rentalTotal - advance;        // 90%

			decimal rentalPart;
			decimal depositPart;
			decimal totalThisTime;

			switch (request.Mode)
			{
				case PaymentType.Deposit:   // LẦN 1: 10% tiền thuê
					rentalPart = advance;
					depositPart = 0;
					totalThisTime = rentalPart;
					break;

				case PaymentType.Rental:    // LẦN 2: 90% + cọc thiết bị
					rentalPart = remainingRental;
					depositPart = deviceDeposit;
					totalThisTime = rentalPart + depositPart;
					break;

				default:
					return BadRequest("Invalid payment mode");
			}

			if (request.Method == PaymentMethod.Wallet)
			{
				// Thanh toán bằng ví
				if (totalThisTime <= 0)
					return BadRequest("No amount to pay by wallet");

				var walletReq = new WalletTransactionRequest
				{
					Amount = totalThisTime,
					Type = request.Mode == PaymentType.Deposit ? "pay_rental_advance" : "pay_rental_and_deposit",
					PaymentId = null,
					BookingId = booking.Id,
					Description = request.Mode == PaymentType.Deposit
						? $"Thanh toán 10% tiền thuê booking {booking.Id} bằng ví"
						: $"Thanh toán 90% tiền thuê + cọc thiết bị booking {booking.Id} bằng ví"
				};

				var ok = await _walletService.DebitAsync(renterId, walletReq);
				if (!ok)
					return BadRequest("Wallet balance not enough");

				var paymentId = await _paymentService.CreateWalletPaymentAsync(
					booking.Id,
					rentalAmount: rentalPart,
					depositAmount: depositPart,
					mode: request.Mode,
					capturedAmount: totalThisTime
				);

				return Ok("Thanh toán bằng ví thành công");
			}
			else
			{
				// Thanh toán qua PayOS (hoặc provider online khác)
				var paymentId = await _paymentService.CreateAuthorizationAsync(
					booking.Id,
					rentalAmount: rentalPart,
					depositAmount: depositPart,
					mode: request.Mode,
					authorizedAmountOverride: totalThisTime
				);

				return Ok(paymentId);
			}
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

			var payment = await _paymentService.GetByIdAsync(id);
			if (payment != null)
			{
				// Thông báo cho renter của booking (nếu lấy được)
				var booking = await _bookingService.GetByIdAsync(payment.BookingId);
				var renterId = booking?.RenterId?.ToString();
				if (!string.IsNullOrEmpty(renterId))
				{
					await _hub.Clients.User(renterId)
						.SendAsync("PaymentUpdated", new
						{
							payment.Id,
							Status = payment.Status.ToString(),
							payment.CapturedAmount,
							payment.RefundedAmount
						});
				}

				// Broadcast cho dashboard Staff/Manager/Admin
				await _hub.Clients.Group("role:Staff")
					.SendAsync("PaymentUpdatedForStaff", new { payment.Id, Status = payment.Status.ToString() });
				await _hub.Clients.Group("role:BranchManager")
					.SendAsync("PaymentUpdatedForManager", new { payment.Id, Status = payment.Status.ToString() });
				await _hub.Clients.Group("role:Admin")
					.SendAsync("PaymentUpdatedForAdmin", new { payment.Id, Status = payment.Status.ToString() });
			}

			return NoContent();
		}

		[HttpPost("{id:guid}/refund")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(
			Summary = "Refund một phần/toàn bộ payment",
			Description = "Thực hiện hoàn tiền thủ công cho renter, cập nhật số tiền đã refund trong payment và cộng tiền về ví của renter.")]
		public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request)
		{
			await _paymentService.RefundAsync(id, request.Amount);
			var payment = await _paymentService.GetByIdAsync(id);
			if (payment != null)
			{
				var booking = await _bookingService.GetByIdAsync(payment.BookingId);
				var renterId = booking?.RenterId?.ToString();
				if (!string.IsNullOrEmpty(renterId))
				{
					// Refund về ví của renter
					var walletRefundReq = new WalletTransactionRequest
					{
						Amount = request.Amount,
						Type = "refund",
						PaymentId = payment.Id,
						BookingId = payment.BookingId,
						Description = $"Hoàn tiền về ví cho payment {payment.Id}"
					};

					await _walletService.CreditAsync(Guid.Parse(renterId), walletRefundReq);

					await _hub.Clients.User(renterId)
						.SendAsync("PaymentUpdated", new
						{
							payment.Id,
							Status = payment.Status.ToString(),
							payment.CapturedAmount,
							payment.RefundedAmount
						});
				}

				await _hub.Clients.Group("role:Staff")
					.SendAsync("PaymentUpdatedForStaff", new { payment.Id, Status = payment.Status.ToString() });
				await _hub.Clients.Group("role:BranchManager")
					.SendAsync("PaymentUpdatedForManager", new { payment.Id, Status = payment.Status.ToString() });
				await _hub.Clients.Group("role:Admin")
					.SendAsync("PaymentUpdatedForAdmin", new { payment.Id, Status = payment.Status.ToString() });
			}

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
			var payment = await _paymentService.GetByIdAsync(id);
			if (payment == null) return NotFound("Payment not found");

			if (payment.AuthorizedAmount <= 0)
				return BadRequest("No amount to pay");

			var shortId = id.ToString("N")[..8];           // 8 chars
			var desc = $"CR-{shortId}";                    // tổng 11 chars

			var url = await _payOsService.CreatePaymentLinkAsync(
				id,
				payment.AuthorizedAmount,
				desc,
				request.ReturnUrl,
				request.CancelUrl,
				HttpContext.RequestAborted);


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


