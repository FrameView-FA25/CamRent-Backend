using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Application.DTOs.WalletDTO;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class WalletsController : ControllerBase
	{
		private readonly IWalletService _walletService;
		private readonly IPaymentService _paymentService;
		private readonly IPayOsService _payOsService;

		public WalletsController(
			IWalletService walletService,
			IPaymentService paymentService,
			IPayOsService payOsService)
		{
			_walletService = walletService;
			_paymentService = paymentService;
			_payOsService = payOsService;
		}

		// GET api/wallets/balance
		[HttpGet("balance")]
		[Authorize]
		public async Task<ActionResult> GetMyBalance()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var balance = await _walletService.GetBalanceAsync(Guid.Parse(userId));
			return Ok(new { balance });
		}

		// GET api/wallets/me - xem ví + lịch sử
		[HttpGet("me")]
		[Authorize]
		public async Task<ActionResult<WalletSummaryResponse>> GetMyWallet()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var summary = await _walletService.GetSummaryAsync(Guid.Parse(userId));
			return Ok(summary);
		}

		// POST api/wallets/topup - tạo link nạp ví qua PayOS
		[HttpPost("topup")]
		[Authorize]
		[SwaggerOperation(
			Summary = "Tạo link nạp ví",
			Description = "Tạo Payment topup ví và sinh link thanh toán PayOS")]
		public async Task<ActionResult> TopupWallet([FromBody] WalletTopupRequest request)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
					   ?? User.FindFirst("sub")?.Value
					   ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
			{
				return Unauthorized("Không xác định được user hiện tại.");
			}

			if (request.Amount <= 0)
			{
				return BadRequest("Số tiền nạp phải lớn hơn 0.");
			}

			// 1) Tạo Payment topup (không gắn booking)
			var paymentId = await _paymentService.CreateTopupPaymentAsync(userId, request.Amount);

			// 2) Tạo link PayOS
			var shortId = paymentId.ToString("N")[..8];
			var desc = $"TOPUP-{shortId}"; // < 25 ký tự

			var redirectUrl = await _payOsService.CreatePaymentLinkAsync(
				paymentId,
				request.Amount,
				desc,
				request.ReturnUrl,
				request.CancelUrl,
				HttpContext.RequestAborted);

			return Ok(new { redirectUrl });
		}
	}
}
