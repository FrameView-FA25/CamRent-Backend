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

		public async Task<Guid> CreateAuthorizationAsync(
				Guid bookingId,
				decimal rentalAmount,
				decimal depositAmount,
				PaymentType mode,
				decimal? authorizedAmountOverride = null)
		{
			// Tính số tiền sẽ thu lần này
			decimal authorizedAmount = authorizedAmountOverride ?? (rentalAmount + depositAmount);

			var payment = new Payment
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Status = PaymentStatus.Authorized,
				AuthorizedAmount = authorizedAmount,
				CapturedAmount = 0,
				RefundedAmount = 0,
				CreatedAt = DateTime.UtcNow,
				// nếu thêm field Kind trong Payment:
				// Kind = mode.ToString()
			};

			await _unitOfWork.Repository<Payment>().AddAsync(payment);

			// Luôn lưu line chi tiết để sau này dùng cho báo cáo / tranh chấp
			if (rentalAmount > 0)
				await AddLineAsync(payment.Id, "rental", rentalAmount);

			if (depositAmount > 0)
				await AddLineAsync(payment.Id, "deposit", depositAmount);

			await _unitOfWork.Complete();
			return payment.Id;
		}


		public async Task AddLineAsync(Guid paymentId, string type, decimal amount)
		{
			var line = new PaymentLine
			{
				Id = Guid.NewGuid(),
				PaymentId = paymentId,
				Type = type,
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
			await _unitOfWork.Complete();

			// Sau khi capture thủ công (không qua PayOS), vẫn tự sinh hợp đồng
			await _contractService.GenerateAndStoreContractAsync(payment.BookingId);

			// Optional: notify renter via email if available (booking must be loaded to get renter email/code)
		}

		public async Task RefundAsync(Guid paymentId, decimal amount)
		{
			var payment = await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId)
				?? throw new InvalidOperationException("Payment not found");
			payment.RefundedAmount += amount;
			payment.Status = PaymentStatus.Refunded;
			await _unitOfWork.Repository<Payment>().UpdateAsync(payment);
			await _unitOfWork.Complete();
			// Optional: email notification
		}

		public async Task<Payment?> GetByIdAsync(Guid paymentId)
		{
			return await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId);
		}
	}
}
