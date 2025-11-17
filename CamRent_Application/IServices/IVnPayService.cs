using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IVnPayService
	{
		// Build a redirect URL for VNPay with signed params
		Task<string> CreatePaymentUrlAsync(Guid paymentId, decimal amount, string orderInfo, string clientIp, CancellationToken ct = default);

		// Validate return query from VNPay and update payment status accordingly
		Task<bool> ProcessReturnAsync(IDictionary<string, string> queryParams, CancellationToken ct = default);

		// Handle VNPay IPN (server-to-server)
		Task<bool> ProcessIpnAsync(IDictionary<string, string> queryParams, CancellationToken ct = default);
	}
}


