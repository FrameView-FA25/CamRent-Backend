using CamRent_Application.IServices;
using CamRent_Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models;
using PayOS.Models.Webhooks;
using Swashbuckle.AspNetCore.Annotations;
using static CamRent_Application.DTOs.WalletDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PayOsWebhookController : ControllerBase
	{
		private readonly IPayOsService _payOsService;
		private readonly IWalletService _walletService;

		public PayOsWebhookController(IPayOsService payOsService, IWalletService walletService)
		{
			_payOsService = payOsService;
			_walletService = walletService;
		}

		[HttpPost]
		[AllowAnonymous]
		[SwaggerOperation(
			Summary = "Webhook PayOS",
			Description = "Nhận callback từ PayOS, xác thực chữ ký và cập nhật trạng thái payment/contract tương ứng.")]
		public async Task<IActionResult> Handle([FromBody] Webhook body)
		{
			var result = await _payOsService.HandleWebhookAsync(body, HttpContext.RequestAborted);

			// Nếu verify fail hoặc payment không tồn tại, vẫn trả 200 cho PayOS nhưng không làm gì
			if (result == null)
				return Ok();

			if (result.Success && result.BookingId == null && result.UserId.HasValue)
			{
				// Đây là Payment topup ví
				var creditReq = new WalletTransactionRequest
				{
					Amount = result.Amount,
					Type = "topup",
					PaymentId = result.PaymentId,
					BookingId = null,
					Description = $"Topup ví qua PayOS (Payment {result.PaymentId})"
				};

				await _walletService.CreditAsync(result.UserId.Value, creditReq);
			}

			// Nếu là payment cho booking (BookingId != null) thì tuỳ logic của bạn, ở đây mình bỏ qua

			return Ok();
		}
	}
}
