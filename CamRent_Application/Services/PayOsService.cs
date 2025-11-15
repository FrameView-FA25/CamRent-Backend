using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CamRent_Application.Services
{
	public sealed class PayOsService : IPayOsService
	{
		private readonly HttpClient _http;
		private readonly PayOsOptions _opts;
		private readonly IUnitOfWork _uow;
		private readonly ILogger<PayOsService> _logger;
		private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

		public PayOsService(HttpClient http, IOptions<PayOsOptions> opts, IUnitOfWork uow, ILogger<PayOsService> logger)
		{
			_http = http;
			_opts = opts.Value;
			_uow = uow;
			_logger = logger;
		}

		public async Task<string> CreatePaymentLinkAsync(Guid paymentId, decimal amount, string description, string returnUrl, string cancelUrl, CancellationToken ct = default)
		{
			// PayOS uses orderCode; we'll use paymentId as numeric hash
			var orderCode = Math.Abs(BitConverter.ToInt32(paymentId.ToByteArray(), 0));
			var body = new
			{
				orderCode,
				amount = (long)amount,
				description = description,
				returnUrl,
				cancelUrl
			};

			var json = JsonSerializer.Serialize(body, JsonOpts);
			var sig = HmacSha256(_opts.ChecksumKey, json);

			using var req = new HttpRequestMessage(HttpMethod.Post, "/v2/payment-requests")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			req.Headers.Add("x-client-id", _opts.ClientId);
			req.Headers.Add("x-api-key", _opts.ApiKey);
			req.Headers.Add("x-signature", sig);

			using var res = await _http.SendAsync(req, ct);
			var txt = await res.Content.ReadAsStringAsync(ct);
			if (!res.IsSuccessStatusCode)
			{
				_logger.LogWarning("PayOS create link failed: {Status} {Body}", (int)res.StatusCode, txt);
				throw new InvalidOperationException("PayOS create link failed");
			}

			using var doc = JsonDocument.Parse(txt);
			var checkoutUrl = doc.RootElement.GetProperty("data").GetProperty("checkoutUrl").GetString()
				?? throw new InvalidOperationException("PayOS response missing checkoutUrl");

			// Optionally record ProviderPaymentId/orderCode
			var payment = await _uow.Repository<Payment>().GetByIdAsync(paymentId);
			if (payment != null)
			{
				payment.Provider = "PayOS";
				payment.ProviderPaymentId = orderCode.ToString();
				await _uow.Repository<Payment>().UpdateAsync(payment);
				await _uow.Complete();
			}

			return checkoutUrl;
		}

		public async Task<bool> HandleWebhookAsync(IDictionary<string, object> payload, string? signature, CancellationToken ct = default)
		{
			// Verify signature (body JSON)
			var json = JsonSerializer.Serialize(payload, JsonOpts);
			var expected = HmacSha256(_opts.ChecksumKey, json);
			if (!string.Equals(signature, expected, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogWarning("PayOS webhook signature invalid");
				return false;
			}

			// Extract orderCode and status (schema may need adjustment to real PayOS payload)
			if (!payload.TryGetValue("data", out var dataObj) || dataObj is not JsonElement dataEl)
			{
				dataEl = JsonDocument.Parse(json).RootElement.GetProperty("data");
			}

			var orderCode = dataEl.GetProperty("orderCode").GetInt32();
			var status = dataEl.GetProperty("status").GetString();
			var amount = dataEl.GetProperty("amount").GetDecimal();

			// Map back to Payment via ProviderPaymentId
			var payments = await _uow.Repository<Payment>().ListAsync(p => p.Provider == "PayOS" && p.ProviderPaymentId == orderCode.ToString());
			var payment = payments.FirstOrDefault();
			if (payment == null)
				return false;

			var ev = new PaymentEvent
			{
				Id = Guid.NewGuid(),
				PaymentId = payment.Id,
				Provider = "PayOS",
				Type = "webhook",
				Status = status ?? "unknown",
				RawData = json,
				Amount = amount,
				CreatedAt = DateTime.UtcNow
			};
			await _uow.Repository<PaymentEvent>().AddAsync(ev);

			if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
			{
				payment.Status = PaymentStatus.Captured;
				payment.CapturedAmount = amount;
				await _uow.Repository<Payment>().UpdateAsync(payment);
			}

			await _uow.Complete();
			return true;
		}

		private static string HmacSha256(string key, string data)
		{
			using var h = new HMACSHA256(Encoding.UTF8.GetBytes(key));
			var bytes = h.ComputeHash(Encoding.UTF8.GetBytes(data));
			return BitConverter.ToString(bytes).Replace("-", string.Empty);
		}
	}
}


