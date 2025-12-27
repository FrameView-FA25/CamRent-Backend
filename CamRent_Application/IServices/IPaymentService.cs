using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IPaymentService
	{
		Task<Guid> CreateAuthorizationAsync(Guid bookingId,decimal rentalAmount,decimal depositAmount,PaymentType mode,decimal? authorizedAmountOverride = null);

		Task AddLineAsync(Guid paymentId, string type, decimal amount);
		Task<Payment?> GetByIdAsync(Guid paymentId);
		Task<Payment?> GetDepositPaymentWithLinesAsync(Guid bookingId);
		Task<bool> RefundDepositAsync(Guid bookingId, PaymentMethod method, decimal refundAmount);

		Task<Guid> CreateTopupPaymentAsync(Guid userId, decimal amount);
		Task<Guid> CreatePaymentAsync(
			Guid bookingId,
			decimal rentalAmount,
			decimal depositAmount,
			PaymentType mode,
			PaymentMethod method,
			decimal capturedAmount);
	}
}
