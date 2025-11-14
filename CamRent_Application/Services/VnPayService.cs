using System.Security.Cryptography;
using System.Text;
using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.Extensions.Options;

namespace CamRent_Application.Services
{
	public class VnPayService : IVnPayService
	{
		private readonly IUnitOfWork _uow;
		private readonly VnPayOptions _options;

		public VnPayService(IUnitOfWork uow, IOptions<VnPayOptions> options)
		{
			_uow = uow;
			_options = options.Value;
		}

		public Task<string> CreatePaymentUrlAsync(Guid paymentId, decimal amount, string orderInfo, string clientIp, CancellationToken ct = default)
		{
			// VNPay expects amount in VND * 100
			var vnpAmount = ((long)(amount * 100)).ToString();
			var vnpTxnRef = paymentId.ToString("N");
			var vnpOrderInfo = orderInfo;

			var dict = new SortedDictionary<string, string>
			{
				{ "vnp_Version", "2.1.0" },
				{ "vnp_Command", "pay" },
				{ "vnp_TmnCode", _options.TmnCode },
				{ "vnp_Amount", vnpAmount },
				{ "vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss") },
				{ "vnp_CurrCode", "VND" },
				{ "vnp_IpAddr", clientIp },
				{ "vnp_Locale", "vn" },
				{ "vnp_OrderInfo", vnpOrderInfo },
				{ "vnp_OrderType", "other" },
				{ "vnp_ReturnUrl", _options.ReturnUrl },
				{ "vnp_TxnRef", vnpTxnRef }
			};

			var query = BuildQuery(dict);
			var signData = BuildDataToSign(dict);
			var secureHash = HmacSHA512(_options.HashSecret, signData);
			var url = $"{_options.VnpUrl}?{query}&vnp_SecureHash={secureHash}";
			return Task.FromResult(url);
		}

		public async Task<bool> ProcessReturnAsync(IDictionary<string, string> queryParams, CancellationToken ct = default)
		{
			// Extract and verify secure hash
			var receivedHash = queryParams.ContainsKey("vnp_SecureHash") ? queryParams["vnp_SecureHash"] : string.Empty;
			var sorted = new SortedDictionary<string, string>(queryParams
				.Where(kv => !string.Equals(kv.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase) && kv.Key.StartsWith("vnp_"))
				.ToDictionary(kv => kv.Key, kv => kv.Value));

			var dataToSign = BuildDataToSign(sorted);
			var computed = HmacSHA512(_options.HashSecret, dataToSign);
			if (!string.Equals(receivedHash, computed, StringComparison.OrdinalIgnoreCase))
				return false;

			// VNPay result
			var responseCode = queryParams.TryGetValue("vnp_ResponseCode", out var code) ? code : string.Empty;
			var txnRef = queryParams.TryGetValue("vnp_TxnRef", out var refValue) ? refValue : string.Empty;

			if (!Guid.TryParseExact(txnRef, "N", out var paymentId)) return false;
			var payment = await _uow.Repository<Payment>().GetByIdAsync(paymentId);
			if (payment == null) return false;

			if (responseCode == "00")
			{
				// Mark captured full authorized amount (simplified)
				payment.Status = PaymentStatus.Captured;
				payment.CapturedAmount = payment.AuthorizedAmount;
				await _uow.Repository<Payment>().UpdateAsync(payment);
				await _uow.Complete();
				return true;
			}
			else
			{
				// Optionally record failed
				return false;
			}
		}

		// Server-to-server IPN with idempotency
		public async Task<bool> ProcessIpnAsync(IDictionary<string, string> queryParams, CancellationToken ct = default)
		{
			// Canonicalize and verify hash
			var receivedHash = queryParams.TryGetValue("vnp_SecureHash", out var h) ? h : string.Empty;
			var sorted = new SortedDictionary<string, string>(queryParams
				.Where(kv => !string.Equals(kv.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase) && kv.Key.StartsWith("vnp_"))
				.ToDictionary(kv => kv.Key, kv => kv.Value));
			var dataToSign = BuildDataToSign(sorted);
			var computed = HmacSHA512(_options.HashSecret, dataToSign);
			var requestHash = Sha256(dataToSign);

			// Idempotency
			var existing = (await _uow.Repository<PaymentEvent>().ListAsync(e => e.RequestHash == requestHash)).FirstOrDefault();
			if (existing != null)
			{
				return string.Equals(existing.Status, "verified", StringComparison.OrdinalIgnoreCase);
			}

			// Create audit event
			var ev = new PaymentEvent
			{
				Id = Guid.NewGuid(),
				Type = "ipn",
				Status = "received",
				RequestHash = requestHash,
				RawData = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}")),
				ResponseCode = queryParams.TryGetValue("vnp_ResponseCode", out var rc) ? rc : null,
				TransactionNo = queryParams.TryGetValue("vnp_TransactionNo", out var tn) ? tn : null,
				BankCode = queryParams.TryGetValue("vnp_BankCode", out var bc) ? bc : null,
				CardType = queryParams.TryGetValue("vnp_CardType", out var ct2) ? ct2 : null,
				Amount = queryParams.TryGetValue("vnp_Amount", out var am) && long.TryParse(am, out var lam) ? lam / 100m : null,
				CreatedAt = DateTime.UtcNow
			};
			await _uow.Repository<PaymentEvent>().AddAsync(ev);

			// Verify signature
			if (!string.Equals(receivedHash, computed, StringComparison.OrdinalIgnoreCase))
			{
				ev.Status = "failed";
				await _uow.Repository<PaymentEvent>().UpdateAsync(ev);
				await _uow.Complete();
				return false;
			}

			// Extract ids and amount
			var txnRef = queryParams.TryGetValue("vnp_TxnRef", out var refValue) ? refValue : string.Empty;
			if (!Guid.TryParseExact(txnRef, "N", out var paymentId))
			{
				ev.Status = "failed";
				await _uow.Repository<PaymentEvent>().UpdateAsync(ev);
				await _uow.Complete();
				return false;
			}
			ev.PaymentId = paymentId;

			var payment = await _uow.Repository<Payment>().GetByIdAsync(paymentId);
			if (payment == null)
			{
				ev.Status = "failed";
				await _uow.Repository<PaymentEvent>().UpdateAsync(ev);
				await _uow.Complete();
				return false;
			}

			var responseCode = queryParams.TryGetValue("vnp_ResponseCode", out var code) ? code : string.Empty;
			var amountOk = ev.Amount.HasValue && ev.Amount.Value == payment.AuthorizedAmount;

			if (responseCode == "00" && amountOk)
			{
				payment.Status = PaymentStatus.Captured;
				payment.CapturedAmount = payment.AuthorizedAmount;
				await _uow.Repository<Payment>().UpdateAsync(payment);
				ev.Status = "verified";
			}
			else
			{
				ev.Status = "failed";
			}

			await _uow.Repository<PaymentEvent>().UpdateAsync(ev);
			await _uow.Complete();
			return ev.Status == "verified";
		}

		private static string BuildQuery(SortedDictionary<string, string> dict)
		{
			return string.Join("&", dict.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
		}

		private static string BuildDataToSign(SortedDictionary<string, string> dict)
		{
			return string.Join("&", dict.Select(kv => $"{kv.Key}={kv.Value}"));
		}

		private static string HmacSHA512(string key, string data)
		{
			using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
			var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
			return BitConverter.ToString(hash).Replace("-", string.Empty);
		}

		private static string Sha256(string data)
		{
			using var sha = SHA256.Create();
			var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
			return BitConverter.ToString(bytes).Replace("-", string.Empty);
		}
	}
}


