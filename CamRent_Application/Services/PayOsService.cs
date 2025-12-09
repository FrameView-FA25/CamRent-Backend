using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.PayOsWebhookDTO;

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
			Amount = (int)amount,
			Description = description, // "TOPUP-XXXXXXXX"
			ReturnUrl = returnUrl,
			CancelUrl = cancelUrl
		};

		var res = await _client.PaymentRequests.CreateAsync(req);

		var checkoutUrl = res.CheckoutUrl
			?? throw new InvalidOperationException("PayOS response missing checkoutUrl");

		var payment = await _uow.Repository<Payment>().GetByIdAsync(paymentId);
		if (payment != null)
		{
			// Provider đã set khi tạo, nhưng set lại cũng không sao
			payment.Provider = "PayOS";
			payment.ProviderPaymentId = orderCode.ToString();

			await _uow.Repository<Payment>().UpdateAsync(payment);
			await _uow.Complete();
		}

		return checkoutUrl;
	}


	public async Task<PayOsWebhookResult?> HandleWebhookAsync(Webhook webhook, CancellationToken ct = default)
	{
		WebhookData data;
		try
		{
			data = await _client.Webhooks.VerifyAsync(webhook);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Invalid PayOS webhook");
			return null;
		}

		var orderCode = data.OrderCode;
		var amount = data.Amount;

		// CHỈ dùng code của PayOS để xác định thành công
		var isPaid = data.Code == "00"; // hoặc nếu SDK có data.Status == "PAID" thì dùng thêm

		var paymentRepo = _uow.Repository<Payment>();

		var payments = await paymentRepo
			.ListAsync(p => p.Provider == "PayOS"
						 && p.ProviderPaymentId == orderCode.ToString());

		var payment = payments.FirstOrDefault();
		if (payment == null)
		{
			_logger.LogWarning("Payment not found for PayOS orderCode {OrderCode}", orderCode);
			return null;
		}

		// Log event webhook
		var ev = new PaymentEvent
		{
			Id = Guid.NewGuid(),
			PaymentId = payment.Id,
			Provider = "PayOS",
			Type = "webhook",
			Status = isPaid ? "PAID" : "FAILED",
			RawData = System.Text.Json.JsonSerializer.Serialize(webhook),
			Amount = amount,
			CreatedAt = DateTime.UtcNow
		};
		await _uow.Repository<PaymentEvent>().AddAsync(ev);

		var alreadyCaptured = payment.Status == PaymentStatus.Captured;

		if (isPaid && !alreadyCaptured)
		{
			// Update Payment sang Captured
			payment.Status = PaymentStatus.Captured;
			payment.CapturedAmount = amount;

			await paymentRepo.UpdateAsync(payment);

			// Nếu là Payment cho Booking thì confirm booking
			if (payment.BookingId.HasValue)
			{
				var bookingRepo = _uow.Repository<Booking>();
				var booking = await bookingRepo.GetByIdAsync(payment.BookingId.Value);
				if (booking != null && booking.Status == BookingStatus.PendingApproval)
				{
					booking.Status = BookingStatus.Confirmed;
					await bookingRepo.UpdateAsync(booking);
				}
			}
		}

		await _uow.Complete();

		return new PayOsWebhookResult
		{
			// CHỈ Success = true khi đây là lần đầu capture thành công
			Success = isPaid && !alreadyCaptured,
			PaymentId = payment.Id,
			UserId = payment.CreatedByUserId,
			BookingId = payment.BookingId,
			Amount = amount,
			Purpose = payment.Purpose
		};
	}

}
