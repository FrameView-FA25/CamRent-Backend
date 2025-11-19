using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PayOsWebhookController : ControllerBase
	{
		private readonly IPayOsService _payOs;
		public PayOsWebhookController(IPayOsService payOs) { _payOs = payOs; }

		[HttpPost]
		[AllowAnonymous]
		[SwaggerOperation(
			Summary = "Webhook PayOS",
			Description = "Nhận callback từ PayOS, xác thực chữ ký và cập nhật trạng thái payment/contract tương ứng.")]
		public async Task<IActionResult> Handle([FromBody] JsonElement body, [FromHeader(Name = "x-signature")] string? signature)
		{
			var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(body.GetRawText()) ?? new();
			var ok = await _payOs.HandleWebhookAsync(dict, signature, HttpContext.RequestAborted);
			return Ok(new { ok });
		}
	}
}


