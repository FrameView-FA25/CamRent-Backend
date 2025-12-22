using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Application.DTOs.InspectionChecklistDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/inspection-checklists")]
	public class InspectionChecklistsController : ControllerBase
	{
		private readonly IInspectionChecklistService _checklistService;

		public InspectionChecklistsController(IInspectionChecklistService checklistService)
		{
			_checklistService = checklistService;
		}

		// Staff: load active template for rendering checklist UI
		[HttpGet("active")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Lấy checklist template đang active", Description = "Trả về template checklist theo ItemType + InspectionType.")]
		public async Task<IActionResult> GetActive([FromQuery] ItemType itemType, [FromQuery] InspectionType? inspectionType)
		{
			var template = await _checklistService.GetActiveTemplateAsync(itemType, inspectionType);
			if (template == null) return NotFound(new { Message = "Không tìm thấy checklist template đang active." });
			return Ok(template);
		}

		// Admin: list templates
		[HttpGet]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Danh sách checklist templates", Description = "Admin xem danh sách template checklist.")]
		public async Task<IActionResult> List([FromQuery] ItemType? itemType, [FromQuery] InspectionType? inspectionType)
		{
			var list = await _checklistService.ListTemplatesAsync(itemType, inspectionType);
			return Ok(list);
		}

		// Admin: get template detail
		[HttpGet("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Chi tiết checklist template", Description = "Admin xem chi tiết template checklist.")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var template = await _checklistService.GetTemplateByIdAsync(id);
			if (template == null) return NotFound();
			return Ok(template);
		}

		// Admin: create template
		[HttpPost]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Tạo checklist template", Description = "Admin tạo template checklist (kèm sections/items).")]
		public async Task<IActionResult> Create([FromBody] UpsertChecklistTemplateRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var id = await _checklistService.CreateTemplateAsync(request, Guid.Parse(userId!));
			return Ok(new { Id = id });
		}

		// Admin: update template
		[HttpPut("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Cập nhật checklist template", Description = "Admin cập nhật template checklist (replace toàn bộ cấu trúc sections/items).")]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpsertChecklistTemplateRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var updated = await _checklistService.UpdateTemplateAsync(id, request, Guid.Parse(userId!));
			if (updated <= 0) return NotFound();
			return Ok(new { Message = "Updated." });
		}

		// Admin: activate/deactivate
		public class SetActiveRequest { public bool IsActive { get; set; } }

		[HttpPatch("{id:guid}/active")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Bật/tắt template", Description = "Admin bật/tắt template. Khi bật, các template khác cùng (ItemType, InspectionType) sẽ bị tắt.")]
		public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var updated = await _checklistService.SetActiveAsync(id, request.IsActive, Guid.Parse(userId!));
			if (updated <= 0) return NotFound();
			return Ok(new { Message = "Updated." });
		}

		// Admin: delete template
		[HttpDelete("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Xóa checklist template", Description = "Admin xóa template checklist.")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var deleted = await _checklistService.DeleteTemplateAsync(id);
			if (deleted <= 0) return NotFound();
			return Ok(new { Message = "Deleted." });
		}
	}
}
