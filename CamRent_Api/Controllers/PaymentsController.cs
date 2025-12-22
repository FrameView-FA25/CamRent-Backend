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
		private readonly IPayOsService _payOsService;
		private readonly IBookingService _bookingService;
		private readonly IWalletService _walletService;
		private readonly IContractService _contractService;
		private readonly IDisputeService _disputeService;
		private readonly IHubContext<NotificationHub> _hub;

		public PaymentsController(
			IPaymentService paymentService,
			IPayOsService payOsService,
			IBookingService bookingService,
			IWalletService walletService,
			IDisputeService disputeService,
			IContractService contractService,
			IHubContext<NotificationHub> hub)
		{
			_paymentService = paymentService;
			_payOsService = payOsService;
			_bookingService = bookingService;
			_walletService = walletService;
			_contractService = contractService;
			_hub = hub;
			_disputeService = disputeService;
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
					return BadRequest("Không có số tiền nào cần thanh toán");

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
				{
					await _contractService.DeleteBookingContractAsync(booking.Id);
					return BadRequest("Số dư ví không đủ");
				}

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



		[HttpPost("refund")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(
			Summary = "Hoàn cọc hoặc tạo payment bù tranh chấp",
			Description = "Nếu tiền cọc - tiền tranh chấp > 0 thì cập nhật refund cho payment cọc và thêm line refund. Nếu < 0 thì tạo payment bù (PayOS/Wallet).")]
		public async Task<IActionResult> Refund([FromBody] RefundRequest request)
		{
			var depositPayment = await _paymentService.GetDepositPaymentWithLinesAsync(request.BookingId);
			if (depositPayment == null)
				return NotFound("Deposit payment not found");

			var depositLine = depositPayment.Lines
				.FirstOrDefault(l => string.Equals(l.Type, "device_deposit", StringComparison.OrdinalIgnoreCase));
			if (depositLine == null)
				return BadRequest("Device deposit line not found");

			var disputeAmount = await _disputeService.CalculateTotalDisputeAmountByBookingIdAsync(request.BookingId);
			var net = depositLine.Amount - disputeAmount;

			if (net > 0)
			{
				var ok = await _paymentService.RefundDepositAsync(request.BookingId, request.Method, net);
				if (!ok)
					return BadRequest("Refund failed.");

				return Ok(new
				{
					type = "refund",
					amount = net,
					paymentId = depositPayment.Id
				});
			}

			if (net < 0)
			{
				var booking = await _bookingService.GetByIdAsync(request.BookingId);
				if (booking?.RenterId == null)
					return NotFound("Booking not found");

				var extra = Math.Abs(net);

				if (request.Method == PaymentMethod.PayOs)
				{
					var paymentId = await _paymentService.CreateAuthorizationAsync(
						booking.Id,
						rentalAmount: 0,
						depositAmount: 0,
						mode: PaymentType.Offset,
						authorizedAmountOverride: extra);

					return Ok(new
					{
						type = "offset",
						amount = extra,
						paymentId
					});
				}
				if(request.Method == PaymentMethod.Cash)
				{
					var paymentId = await _paymentService.CreatePaymentAsync(
						booking.Id,
						rentalAmount: 0,
						depositAmount: 0,
						mode: PaymentType.Offset,
						method: PaymentMethod.Cash,
						capturedAmount: extra
					);
					if (paymentId == Guid.Empty)
					{
						return StatusCode(StatusCodes.Status400BadRequest, "Tạo payment thất bại.");
					}
					return Ok(new
					{
						type = "offset",
						amount = extra,
						paymentId
					});
				}
			}

			return Ok(new
			{
				type = "none",
				amount = 0m
			});
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


