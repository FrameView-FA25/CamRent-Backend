using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class PaymentService : IPaymentService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailService? _email;
		private readonly IContractService _contractService;

		public PaymentService(IUnitOfWork unitOfWork, IContractService contractService, IEmailService? email = null)
		{
			_unitOfWork = unitOfWork;
			_contractService = contractService;
			_email = email;
		}

		// TOPUP VÍ (PAYOS)
		public async Task<Guid> CreateTopupPaymentAsync(Guid userId, decimal amount)
		{
			var payment = new Payment
			{
				Id = Guid.NewGuid(),
				BookingId = null,
				CreatedByUserId = userId,
				Status = PaymentStatus.Authorized,
				Provider = "PayOS",
				Purpose = "wallet_topup",
				AuthorizedAmount = amount,
				CapturedAmount = 0,
				RefundedAmount = 0,
				CreatedAt = DateTime.UtcNow
			};

			await _unitOfWork.Repository<Payment>().AddAsync(payment);
			await _unitOfWork.Complete();

			return payment.Id;
		}

		// THANH TOÁN BẰNG VÍ (ĐÃ CAPTURE LUÔN) - DÙNG CHO 10% HOẶC 90%+CỌC TUỲ CÁCH GỌI
		public async Task<Guid> CreateWalletPaymentAsync(
			Guid bookingId,
			decimal rentalAmount,     // phần tiền thuê trong lần này (10% hoặc 90%)
			decimal depositAmount,    // cọc thiết bị (nếu có, thường chỉ ở lần 2)
			PaymentType mode,
			decimal capturedAmount)
		{
			var payment = new Payment
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Status = PaymentStatus.Captured,
				Provider = "Wallet",
				Purpose = "booking",
				AuthorizedAmount = capturedAmount,
				CapturedAmount = capturedAmount,
				RefundedAmount = 0,
				CreatedAt = DateTime.UtcNow
			};

			await _unitOfWork.Repository<Payment>().AddAsync(payment);

			// ---------- LINE CHI TIẾT ----------
			// Nếu mode = Deposit  -> rentalAmount = 10%  -> line "rental_advance"
			// Nếu mode = Rental   -> rentalAmount = 90%  -> line "rental"
			if (rentalAmount > 0)
			{
				var rentalLineType = mode == PaymentType.Deposit
					? "rental_advance"
					: "rental";

				await AddLineAsync(payment.Id, rentalLineType, rentalAmount);
			}

			// depositAmount = cọc thiết bị (device deposit)
			if (depositAmount > 0)
				await AddLineAsync(payment.Id, "device_deposit", depositAmount);

			// ---------- CẬP NHẬT BOOKING ----------
			var bookingRepo = _unitOfWork.Repository<Booking>();
			var booking = await bookingRepo.GetByIdAsync(bookingId);
			if (booking != null && booking.Status == BookingStatus.PendingApproval)
			{
				booking.Status = BookingStatus.Confirmed;
				await bookingRepo.UpdateAsync(booking);
			}

			await _unitOfWork.Complete();
			return payment.Id;
		}

		// THANH TOÁN ONLINE (PAYOS...) - TẠO PAYMENT AUTHORIZIED
		public async Task<Guid> CreateAuthorizationAsync(
			Guid bookingId,
			decimal rentalAmount,     // phần tiền thuê trong lần này (10% hoặc 90%)
			decimal depositAmount,    // cọc thiết bị (nếu có)
			PaymentType mode,
			decimal? authorizedAmountOverride = null)
		{
			// Số tiền thu lần này = phần thuê + cọc thiết bị (nếu có)
			decimal authorizedAmount = authorizedAmountOverride ?? (rentalAmount + depositAmount);

			var payment = new Payment
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Status = PaymentStatus.Authorized,
				Provider = "PayOS",
				Purpose = "booking",
				AuthorizedAmount = authorizedAmount,
				CapturedAmount = 0,
				RefundedAmount = 0,
				CreatedAt = DateTime.UtcNow
			};

			await _unitOfWork.Repository<Payment>().AddAsync(payment);

			// ---------- LINE CHI TIẾT ----------
			if (rentalAmount > 0)
			{
				var rentalLineType = mode == PaymentType.Deposit
					? "rental_advance"
					: "rental";

				await AddLineAsync(payment.Id, rentalLineType, rentalAmount);
			}

			if (depositAmount > 0)
				await AddLineAsync(payment.Id, "device_deposit", depositAmount);

			await _unitOfWork.Complete();
			return payment.Id;
		}

		public async Task AddLineAsync(Guid paymentId, string type, decimal amount)
		{
			var line = new PaymentLine
			{
				Id = Guid.NewGuid(),
				PaymentId = paymentId,
				Type = type,                 // rental_advance / rental / device_deposit / ...
				Amount = amount,
				CapturedAmount = 0,
				RefundedAmount = 0,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<PaymentLine>().AddAsync(line);
		}

		public async Task CaptureAsync(Guid paymentId, decimal amount)
		{
			var payment = await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId)
				?? throw new InvalidOperationException("Payment not found");

			payment.CapturedAmount += amount;
			payment.Status = PaymentStatus.Captured;
			await _unitOfWork.Repository<Payment>().UpdateAsync(payment);

			// Nếu payment gắn booking thì Confirm booking
			if (payment.BookingId.HasValue)
			{
				var bookingRepo = _unitOfWork.Repository<Booking>();
				var booking = await bookingRepo.GetByIdAsync(payment.BookingId.Value);
				if (booking != null && booking.Status == BookingStatus.PendingApproval)
				{
					booking.Status = BookingStatus.Confirmed;
					await bookingRepo.UpdateAsync(booking);
				}
			}

			await _unitOfWork.Complete();
		}

		public async Task RefundAsync(Guid paymentId, decimal amount)
		{
			var payment = await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId)
				?? throw new InvalidOperationException("Payment not found");

			payment.RefundedAmount += amount;
			payment.Status = PaymentStatus.Refunded;
			await _unitOfWork.Repository<Payment>().UpdateAsync(payment);
			await _unitOfWork.Complete();
			// TODO: gửi email nếu cần
		}

		public async Task<Payment?> GetByIdAsync(Guid paymentId)
		{
			return await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId);
		}
	}
}
