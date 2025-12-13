using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static CamRent_Application.DTOs.WorkSlotDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class WorkSlotsController : ControllerBase
	{
		private readonly IWorkSlotService _service;

		public WorkSlotsController(IWorkSlotService service)
		{
			_service = service;
		}

		/// <summary>
		/// Danh sách cấu hình slot làm việc (áp dụng toàn hệ thống).
		/// </summary>
		[HttpGet]
		[Authorize] // bất kỳ user đăng nhập nào cũng có thể xem cấu hình slot
		[SwaggerOperation(
			Summary = "Danh sách slot làm việc",
			Description = "Trả về danh sách khung giờ (slot) làm việc để FE hiển thị timetable.")]
		public async Task<ActionResult<IEnumerable<WorkSlotResponse>>> GetSlots()
		{
			var slots = await _service.GetSlotsAsync(HttpContext.RequestAborted);
			return Ok(slots);
		}

		/// <summary>
		/// Tạo mới một slot làm việc. Chỉ Admin có quyền sửa cấu hình slot.
		/// </summary>
		[HttpPost]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(
			Summary = "Tạo slot làm việc",
			Description = "Admin cấu hình slot mới (slotIndex, giờ bắt đầu/kết thúc).")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateWorkSlotRequest request)
		{
			var id = await _service.CreateAsync(request, HttpContext.RequestAborted);
			return Ok(id);
		}

		/// <summary>
		/// Cập nhật thời gian / trạng thái một slot làm việc.
		/// </summary>
		[HttpPut("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Cập nhật slot làm việc", Description = "Admin cập nhật thời lượng hoặc bật/tắt một slot.")]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkSlotRequest request)
		{
			var ok = await _service.UpdateAsync(id, request, HttpContext.RequestAborted);
			if (!ok) return NotFound();
			return NoContent();
		}

		/// <summary>
		/// Xoá một slot làm việc.
		/// </summary>
		[HttpDelete("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Xoá slot làm việc", Description = "Admin xoá cấu hình slot không còn dùng nữa.")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var ok = await _service.DeleteAsync(id, HttpContext.RequestAborted);
			if (!ok) return NotFound();
			return NoContent();
		}
	}
}


