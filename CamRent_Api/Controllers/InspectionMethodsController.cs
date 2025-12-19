using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Application.DTOs.InspectionChecklistDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/inspection-methods")]
	public class InspectionMethodsController : ControllerBase
	{
		private readonly IInspectionMethodService _service;

		public InspectionMethodsController(IInspectionMethodService service)
		{
			_service = service;
		}

		[HttpGet]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Danh sách methods", Description = "Admin lấy danh sách methods để cấu hình checklist template.")]
		public async Task<IActionResult> List([FromQuery] bool includeInactive = false)
		{
			var list = await _service.ListAsync(includeInactive);
			return Ok(list);
		}

		[HttpGet("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var method = await _service.GetByIdAsync(id);
			if (method == null) return NotFound();
			return Ok(method);
		}

		[HttpPost]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> Create([FromBody] UpsertInspectionMethodRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var id = await _service.CreateAsync(request, Guid.Parse(userId!));
			return Ok(new { Id = id });
		}

		[HttpPut("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpsertInspectionMethodRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var updated = await _service.UpdateAsync(id, request, Guid.Parse(userId!));
			if (updated <= 0) return NotFound();
			return Ok(new { Message = "Updated." });
		}

		[HttpDelete("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var deleted = await _service.DeleteAsync(id);
			if (deleted <= 0) return NotFound();
			return Ok(new { Message = "Deleted." });
		}
	}
}

