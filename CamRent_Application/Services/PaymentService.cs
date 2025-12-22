using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static QuestPDF.Helpers.Colors;

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

		// THANH TOÁN BẰNG VÍ VÀ TIỀN MẶT (ĐÃ CAPTURE LUÔN) - DÙNG CHO 10% HOẶC 90%+CỌC TUỲ CÁCH GỌI
		public async Task<Guid> CreatePaymentAsync(
			Guid bookingId,
			decimal rentalAmount,     // phần tiền thuê trong lần này (10% hoặc 90%)
			decimal depositAmount,    // cọc thiết bị (nếu có, thường chỉ ở lần 2)
			PaymentType mode,
			PaymentMethod method,
			decimal capturedAmount)
		{
			var purpose = mode == PaymentType.Offset ? "dispute_offset" : "booking";
			var payment = new Payment
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Status = PaymentStatus.Captured,
				Provider = method.ToString(),
				Purpose = purpose,
				AuthorizedAmount = capturedAmount,
				CapturedAmount = capturedAmount,
				RefundedAmount = 0,
				CreatedAt = DateTime.UtcNow
			};

			await _unitOfWork.Repository<Payment>().AddAsync(payment);

			// ---------- LINE CHI TIẾT ----------
			if (mode == PaymentType.Offset)
			{
				if (capturedAmount > 0)
					await AddLineAsync(payment.Id, "offset", capturedAmount);
			}
			else
			{
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
			}

			// ---------- CẬP NHẬT BOOKING ----------
			if (mode == PaymentType.Deposit)
			{
				var bookingRepo = _unitOfWork.Repository<Booking>();
				var booking = await bookingRepo.GetByIdAsync(bookingId);
				if (booking != null)
				{
					booking.Status = BookingStatus.Confirmed;
					await bookingRepo.UpdateAsync(booking);
				}
				try
				{
					await TryFinalizeContractForDepositAsync(bookingId);
				}
				catch (Exception ex)
				{
					// Log lỗi nhưng không làm gián đoạn luồng chính
					// Giả sử có _logger
					// _logger.LogError(ex, "Failed to finalize contract for Booking {BookingId}", bookingId);
				}
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
			string purpose = "booking";
			if (mode == PaymentType.Offset)
			{
				purpose = "dispute_offset";
			}
			else if (depositAmount == 0)
			{
				purpose = "reserve";
			}
			var payment = new Payment
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Status = PaymentStatus.Authorized,
				Provider = "PayOS",
				Purpose = purpose,
				AuthorizedAmount = authorizedAmount,
				CapturedAmount = 0,
				RefundedAmount = 0,
				CreatedAt = DateTime.UtcNow
			};

			await _unitOfWork.Repository<Payment>().AddAsync(payment);
			// ---------- LINE CHI TIẾT ----------
			if (mode == PaymentType.Offset)
			{
				if (authorizedAmount > 0)
					await AddLineAsync(payment.Id, "offset", authorizedAmount);
			}
			else
			{
				if (rentalAmount > 0)
				{
					var rentalLineType = mode == PaymentType.Deposit
						? "rental_advance"
						: "rental";

					await AddLineAsync(payment.Id, rentalLineType, rentalAmount);
				}

				if (depositAmount > 0)
					await AddLineAsync(payment.Id, "device_deposit", depositAmount);
			}

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

		public async Task<Payment?> GetByIdAsync(Guid paymentId)
		{
			return await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId);
		}

		public async Task<Payment?> GetDepositPaymentWithLinesAsync(Guid bookingId)
		{
			var payments = await _unitOfWork.Repository<Payment>().ListAsync(
				p => p.BookingId == bookingId,
				include: q => q.Include(p => p.Lines));

			return payments.FirstOrDefault(p =>
				p.Lines.Any(l => string.Equals(l.Type, "device_deposit", StringComparison.OrdinalIgnoreCase)));
		}

		public async Task<bool> RefundDepositAsync(Guid bookingId, PaymentMethod method, decimal refundAmount)
		{
			if (refundAmount <= 0)
				throw new ArgumentException("Refund amount must be greater than 0.", nameof(refundAmount));

			var payment = new Payment
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Status = PaymentStatus.Refunded,
				Provider = method.ToString(),
				Purpose = "Hoàn tiền",
				AuthorizedAmount = refundAmount,
				CapturedAmount = 0,
				RefundedAmount = refundAmount,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<Payment>().AddAsync(payment);
			await AddLineAsync(payment.Id, "refund", refundAmount);

			await _unitOfWork.Complete();
			return true;
		}

		private async Task TryFinalizeContractForDepositAsync(Guid bookingId)
		{
			var contractRepo = _unitOfWork.Repository<Contract>();
			var contract = (await contractRepo.ListAsync(
					filter: c => c.BookingId == bookingId,
					include: q => q.Include(c => c.Signatures)))
				.FirstOrDefault();

			if (contract == null || contract.Status == ContractStatus.Completed)
				return;

			var fullContract = await _contractService.GetByIdAsync(contract.Id);
			var signatures = fullContract?.Signatures?.ToList() ?? contract.Signatures.ToList();
			await _contractService.GenerateAndUploadFinalPdfAsync(contract.Id, signatures);
		}
	}
}
