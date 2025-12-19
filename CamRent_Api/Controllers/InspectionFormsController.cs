using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Application.DTOs.InspectionFormDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/inspection-forms")]
	public class InspectionFormsController : ControllerBase
	{
		private readonly IInspectionFormService _service;

		public InspectionFormsController(IInspectionFormService service)
		{
			_service = service;
		}

		[HttpPost]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Tạo phiếu kiểm tra (checklist)", Description = "Tạo 1 phiếu checklist và các dòng inspection tương ứng, trả về formId.")]
		public async Task<IActionResult> Create([FromBody] CreateInspectionFormRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (!ModelState.IsValid) return BadRequest(ModelState);

			var formId = await _service.CreateAsync(request, Guid.Parse(userId!));
			return Ok(new { Id = formId });
		}

		[HttpGet("{id:guid}")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Xem lại phiếu kiểm tra", Description = "Trả về phiếu checklist (header + rows) giống lúc tạo để có thể edit.")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var form = await _service.GetByIdAsync(id);
			if (form == null) return NotFound();
			return Ok(form);
		}

		[HttpGet("booking/{bookingId:guid}")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Danh sách phiếu theo booking", Description = "Trả về các phiếu checklist đã tạo cho một booking.")]
		public async Task<IActionResult> ListByBooking(Guid bookingId)
		{
			var list = await _service.ListByBookingAsync(bookingId);
			return Ok(list);
		}

		[HttpGet("verification/{verificationId:guid}")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Danh sách phiếu theo verification", Description = "Trả về các phiếu checklist đã tạo cho một verification request.")]
		public async Task<IActionResult> ListByVerification(Guid verificationId)
		{
			var list = await _service.ListByVerificationAsync(verificationId);
			return Ok(list);
		}

		[HttpPut("{id:guid}")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Cập nhật phiếu kiểm tra", Description = "Cập nhật pass/notes/methods cho các dòng trong phiếu.")]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInspectionFormRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (!ModelState.IsValid) return BadRequest(ModelState);

			var updated = await _service.UpdateAsync(id, request, Guid.Parse(userId!));
			if (updated <= 0) return NotFound();
			return Ok(new { Message = "Updated." });
		}
	}
}
