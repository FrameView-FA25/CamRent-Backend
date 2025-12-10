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
		public class ProcessWithdrawRequest
		{
			public string? Note { get; set; }
		}

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

		[HttpGet("balance")]
		[Authorize]
		public async Task<ActionResult> GetMyBalance()
		{
			// Lấy số dư ví hiện tại của user đăng nhập.
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var balance = await _walletService.GetBalanceAsync(Guid.Parse(userId));
			return Ok(new { balance });
		}

		[HttpGet("me")]
		[Authorize]
		public async Task<ActionResult<WalletSummaryResponse>> GetMyWallet()
		{
			// Lấy số dư ví + một số transaction gần nhất để hiển thị lịch sử.
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var summary = await _walletService.GetSummaryAsync(Guid.Parse(userId));
			return Ok(summary);
		}

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

		/// <summary>
		/// Người dùng gửi yêu cầu rút tiền từ ví về tài khoản ngân hàng đã khai báo trong hồ sơ.
		/// Hệ thống chỉ chấp nhận nếu số tiền yêu cầu nhỏ hơn hoặc bằng số dư hiện có trong ví.
		/// Yêu cầu này chỉ được log lại trong WalletTransaction (type = withdraw_request),
		/// việc chuyển tiền thực tế sẽ do Staff xử lý thủ công bằng QR để tránh nhầm lẫn.
		/// </summary>
		[HttpPost("withdraw")]
		[Authorize]
		[SwaggerOperation(
			Summary = "Gửi yêu cầu rút tiền từ ví",
			Description = "User nhập số tiền muốn rút; hệ thống kiểm tra số dư, trừ tiền khỏi ví và tạo một WalletTransaction 'withdraw_request' để staff xử lý thủ công.")]
		public async Task<IActionResult> RequestWithdraw([FromBody] WalletWithdrawRequest request)
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
				return BadRequest("Số tiền rút phải lớn hơn 0.");
			}

			// Không cho tạo yêu cầu mới nếu đang có tiền bị giữ (FrozenBalance > 0),
			// tương đương với đang có yêu cầu rút trước đó chưa được staff xử lý xong.
			var summary = await _walletService.GetSummaryAsync(userId);
			if (summary.FrozenBalance > 0)
			{
				return BadRequest("Bạn đang có một yêu cầu rút tiền đang chờ xử lý, vui lòng đợi staff hoàn tất trước khi tạo yêu cầu mới.");
			}

			var ok = await _walletService.RequestWithdrawAsync(userId, request.Amount, request.Note);
			if (!ok)
			{
				return BadRequest("Số dư ví không đủ để rút số tiền này.");
			}

			return Ok(new { ok = true });
		}

		/// <summary>
		/// Staff xem danh sách các yêu cầu rút tiền mới nhất để xử lý thủ công.
		/// </summary>
		[HttpGet("withdraw/history")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(
			Summary = "Danh sách yêu cầu rút tiền",
			Description = "Trả về một số dòng lịch sử rút tiền (withdraw_request/withdraw/withdraw_failed) để staff tiện theo dõi và xử lý.")]
		public async Task<IActionResult> GetWithdrawHistory([FromQuery] int take = 50)
		{
			var items = await _walletService.GetWithdrawHistoryAsync(take);
			return Ok(items);
		}

		/// <summary>
		/// Staff xác nhận đã chuyển tiền thành công cho một yêu cầu rút cụ thể.
		/// Hệ thống log thêm transaction \"withdraw\" để lịch sử rõ ràng (tiền đã bị trừ ở bước gửi yêu cầu).
		/// </summary>
		[HttpPost("withdraw/{transactionId:guid}/complete")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(
			Summary = "Hoàn tất yêu cầu rút tiền",
			Description = "Staff đánh dấu một yêu cầu rút (withdraw_request) là đã chuyển tiền thành công, không thay đổi số dư ví vì đã trừ khi tạo yêu cầu.")]
		public async Task<IActionResult> CompleteWithdraw(Guid transactionId, [FromBody] ProcessWithdrawRequest request)
		{
			var ok = await _walletService.CompleteWithdrawAsync(transactionId, request.Note);
			if (!ok)
				return BadRequest("Không tìm thấy yêu cầu rút hợp lệ.");

			return Ok(new { ok = true });
		}

		/// <summary>
		/// Staff đánh dấu yêu cầu rút thất bại hoặc bị hủy.
		/// Hệ thống hoàn lại số tiền đã trừ trong ví và log transaction \"withdraw_failed\".
		/// </summary>
		[HttpPost("withdraw/{transactionId:guid}/fail")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(
			Summary = "Hủy/hoàn tiền yêu cầu rút",
			Description = "Staff đánh dấu yêu cầu rút thất bại hoặc hủy, hệ thống hoàn lại tiền vào ví và log 'withdraw_failed'.")]
		public async Task<IActionResult> FailWithdraw(Guid transactionId, [FromBody] ProcessWithdrawRequest request)
		{
			var ok = await _walletService.FailWithdrawAsync(transactionId, request.Note);
			if (!ok)
				return BadRequest("Không tìm thấy yêu cầu rút hợp lệ.");

			return Ok(new { ok = true });
		}
	}
}
