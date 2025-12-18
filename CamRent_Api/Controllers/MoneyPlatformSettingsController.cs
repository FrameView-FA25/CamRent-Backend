using CamRent_Application.Common;
using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Application.DTOs.MoneyPlatformSettingDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Policy = "AdminOnly")]
	public sealed class MoneyPlatformSettingsController : ControllerBase
	{
		private readonly IMoneyPlatformSettingsService _svc;

		public MoneyPlatformSettingsController(IMoneyPlatformSettingsService svc)
		{
			_svc = svc;
		}

		[HttpGet]
		[SwaggerOperation(Summary = "Danh sách cấu hình tiền nền tảng", Description = "Admin xem toàn bộ lịch sử cấu hình MoneyPlatformSettings.")]
		public async Task<IActionResult> GetAll(CancellationToken ct)
		{
			var items = await _svc.GetAllAsync(ct);
			return Ok(items);
		}

		[HttpGet("active")]
		[SwaggerOperation(Summary = "Cấu hình đang active", Description = "Admin lấy bản ghi IsActive=true (mới nhất).")]
		public async Task<IActionResult> GetActive(CancellationToken ct)
		{
			var item = await _svc.GetActiveAsync(ct);
			if (item == null) return NotFound();
			return Ok(item);
		}

		[HttpGet("{id:guid}")]
		[SwaggerOperation(Summary = "Chi tiết cấu hình", Description = "Admin lấy cấu hình theo id.")]
		public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
		{
			var item = await _svc.GetByIdAsync(id, ct);
			if (item == null) return NotFound();
			return Ok(item);
		}

		[HttpPost]
		[SwaggerOperation(Summary = "Tạo cấu hình mới", Description = "Tạo bản ghi cấu hình mới; nếu IsActive=true sẽ tự deactivate các bản ghi active khác.")]
		public async Task<IActionResult> Create([FromBody] MoneyPlatformSettingRequest req, CancellationToken ct)
		{
			try
			{
				var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
							  ?? User.FindFirst("sub")?.Value
							  ?? User.FindFirst("uid")?.Value;
				if (string.IsNullOrEmpty(userIdStr))
					return Unauthorized();

				var id = await _svc.CreateAsync(req, Guid.Parse(userIdStr), ct);
				return Ok(new { id });
			}
			catch (AppException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPut("{id:guid}")]
		[SwaggerOperation(Summary = "Cập nhật cấu hình", Description = "Cập nhật bản ghi; nếu chuyển IsActive=true sẽ tự deactivate các bản ghi active khác.")]
		public async Task<IActionResult> Update(Guid id, [FromBody] MoneyPlatformSettingRequest req, CancellationToken ct)
		{
			try
			{
				var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
							  ?? User.FindFirst("sub")?.Value
							  ?? User.FindFirst("uid")?.Value;
				if (string.IsNullOrEmpty(userIdStr))
					return Unauthorized();

				var ok = await _svc.UpdateAsync(id, req, Guid.Parse(userIdStr), ct);
				if (!ok) return NotFound();
				return NoContent();
			}
			catch (AppException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpDelete("{id:guid}")]
		[SwaggerOperation(Summary = "Xóa cấu hình", Description = "Xóa bản ghi cấu hình theo id.")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
		{
			var ok = await _svc.DeleteAsync(id, ct);
			if (!ok) return NotFound();
			return NoContent();
		}
	}
}

