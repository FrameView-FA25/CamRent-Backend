using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models; // quan trọng: chỉ cần namespace này
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class PayOsService : IPayOsService
{
	private readonly PayOSClient _client;
	private readonly IUnitOfWork _uow;
	private readonly IContractService _contractService;
	private readonly ILogger<PayOsService> _logger;

	public PayOsService(
		IOptions<PayOsOptions> opts,
		IUnitOfWork uow,
		IContractService contractService,
		ILogger<PayOsService> logger)
	{
		var o = opts.Value;
		_client = new PayOSClient(o.ClientId, o.ApiKey, o.ChecksumKey);
		_uow = uow;
		_contractService = contractService;
		_logger = logger;
	}

	public async Task<string> CreatePaymentLinkAsync(
		Guid paymentId,
		decimal amount,
		string description,
		string returnUrl,
		string cancelUrl,
		CancellationToken ct = default)
	{
		// orderCode phải là số nguyên dương
		var orderCode = Math.Abs(BitConverter.ToInt32(paymentId.ToByteArray(), 0));
		if (orderCode == 0) orderCode = 1;

		var req = new CreatePaymentLinkRequest
		{
			OrderCode = orderCode,
			Amount = (int)amount,           // PayOS dùng int VND
			Description = description,
			ReturnUrl = returnUrl,
			CancelUrl = cancelUrl
			// SDK không bắt buộc Items, nên bỏ cho gọn
		};

		// Không có overload nhận CancellationToken, gọi như docs
		var res = await _client.PaymentRequests.CreateAsync(req);

		var checkoutUrl = res.CheckoutUrl
			?? throw new InvalidOperationException("PayOS response missing checkoutUrl");

		// Lưu mapping để webhook tra ngược
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

	public async Task<bool> HandleWebhookAsync(Webhook webhook, CancellationToken ct = default)
	{
		WebhookData data;
		try
		{
			// Verify chữ ký + parse data
			data = await _client.Webhooks.VerifyAsync(webhook);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Invalid PayOS webhook");
			return false;
		}

		var orderCode = data.OrderCode;
		var amount = data.Amount;
		var success = webhook.Success && data.Code == "00";

		var payments = await _uow.Repository<Payment>()
			.ListAsync(p => p.Provider == "PayOS" && p.ProviderPaymentId == orderCode.ToString());
		var payment = payments.FirstOrDefault();
		if (payment == null) return false;

		var ev = new PaymentEvent
		{
			Id = Guid.NewGuid(),
			PaymentId = payment.Id,
			Provider = "PayOS",
			Type = "webhook",
			Status = success ? "PAID" : "FAILED",
			RawData = System.Text.Json.JsonSerializer.Serialize(webhook),
			Amount = amount,
			CreatedAt = DateTime.UtcNow
		};
		await _uow.Repository<PaymentEvent>().AddAsync(ev);

		if (success)
		{
			payment.Status = PaymentStatus.Captured;
			payment.CapturedAmount = amount;
			await _uow.Repository<Payment>().UpdateAsync(payment);
		}

		await _uow.Complete();
		return true;
	}
}
