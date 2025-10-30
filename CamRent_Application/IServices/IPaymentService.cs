using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IPaymentService
	{
		Task<Guid> CreateAuthorizationAsync(Guid bookingId, decimal rentalAmount, decimal depositAmount);
		Task AddLineAsync(Guid paymentId, string type, decimal amount);
		Task CaptureAsync(Guid paymentId, decimal amount);
		Task RefundAsync(Guid paymentId, decimal amount);
	}
}
