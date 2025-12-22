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
		[Authorize(Roles = "Renter,Staff")]
		public async Task<ActionResult<Guid>> Authorize([FromBody] CreateAuthorizationRequest request)
		{
			// Tính số tiền phải thanh toán cho booking theo từng đợt:
			// - Lần 1 (Deposit): thu 10% tiền thuê (advance)
			// - Lần 2 (Rental): thu 90% tiền thuê còn lại + cọc thiết bị
			// Sau đó tuỳ theo phương thức (ví hoặc PayOS) mà trừ ví hoặc tạo Payment chờ thanh toán online.
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

				var paymentId = await _paymentService.CreatePaymentAsync(
					booking.Id,
					rentalAmount: rentalPart,
					depositAmount: depositPart,
					mode: request.Mode,
					method: PaymentMethod.Wallet,
					capturedAmount: totalThisTime
				);
				return Ok("Thanh toán bằng ví thành công");
			}
			else if (request.Method == PaymentMethod.Cash)
			{
				var paymentId = await _paymentService.CreatePaymentAsync(
					booking.Id,
					rentalAmount: rentalPart,
					depositAmount: depositPart,
					mode: request.Mode,
					method: PaymentMethod.Cash,
					capturedAmount: totalThisTime
				);
				if(paymentId == Guid.Empty)
				{
					return StatusCode(StatusCodes.Status400BadRequest, "Tạo payment thất bại.");
				}
				return Ok("Thanh toán bằng tiền mặt thành công");
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

		// Init PayOS payment link
		[HttpPost("{id:guid}/payos")]
		[Authorize(Roles = "Renter,Staff")]
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


			return Ok(new
			{
				paymentId = id,          // thêm cái này
				redirectUrl = url
			});
		}
		[HttpGet("{id:guid}/status")]
		[Authorize(Policy = "Renter")] // hoặc policy rộng hơn tuỳ bạn
		[SwaggerOperation(
	Summary = "Lấy trạng thái payment + booking",
	Description = "Cho mobile kiểm tra trạng thái mới nhất sau khi thanh toán PayOS (dựa trên webhook đã cập nhật DB).")]
		public async Task<ActionResult<PaymentStatusResponse>> GetStatus(Guid id)
		{
			var payment = await _paymentService.GetByIdAsync(id);
			if (payment == null)
				return NotFound("Payment not found");

			var booking = await _bookingService.GetByIdAsync(payment.BookingId);

			var response = new PaymentStatusResponse
			{
				PaymentId = payment.Id,
				PaymentStatus = payment.Status.ToString(),
				BookingId = payment.BookingId,
				BookingStatus = booking?.Status.ToString(),
				AuthorizedAmount = payment.AuthorizedAmount,
				CapturedAmount = payment.CapturedAmount,
				IsPaid = payment.Status == PaymentStatus.Captured
			};

			return Ok(response);
		}
	}
}


