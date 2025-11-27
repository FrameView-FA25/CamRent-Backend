using PayOS.Models; // để dùng Webhook
using PayOS.Models.Webhooks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IPayOsService
	{
		Task<string> CreatePaymentLinkAsync(
			Guid paymentId,
			decimal amount,
			string description,
			string returnUrl,
			string cancelUrl,
			CancellationToken ct = default);

		Task<bool> HandleWebhookAsync(
			Webhook webhook,
			CancellationToken ct = default);
	}
}
