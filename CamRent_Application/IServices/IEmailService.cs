using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IEmailService
	{
		Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);

		// Convenience helpers
		Task SendPaymentCapturedAsync(string to, string bookingCode, decimal amount, CancellationToken ct = default);
		Task SendPaymentRefundedAsync(string to, string bookingCode, decimal amount, CancellationToken ct = default);
	}
}


