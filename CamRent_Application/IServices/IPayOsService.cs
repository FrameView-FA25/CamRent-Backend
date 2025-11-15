using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IPayOsService
	{
		Task<string> CreatePaymentLinkAsync(Guid paymentId, decimal amount, string description, string returnUrl, string cancelUrl, CancellationToken ct = default);
		Task<bool> HandleWebhookAsync(IDictionary<string, object> payload, string? signature, CancellationToken ct = default);
	}
}


