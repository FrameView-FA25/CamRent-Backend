using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models;
using PayOS.Models.Webhooks;
using Swashbuckle.AspNetCore.Annotations;

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
		public async Task<IActionResult> Handle([FromBody] Webhook body)
		{
			var ok = await _payOs.HandleWebhookAsync(body, HttpContext.RequestAborted);
			return Ok(new { ok });
		}
	}
}
