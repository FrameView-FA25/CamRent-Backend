using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class DisputesController : ControllerBase
	{
		private readonly IDisputeService _dispute;
		public DisputesController(IDisputeService dispute) { _dispute = dispute; }

		// Payload khi mở một dispute mới cho một booking (do Staff tạo dựa trên phản ánh của renter/owner).
		public sealed class OpenRequest
		{
			public Guid BookingId { get; set; }
			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Severity { get; set; } = "minor";
		}

		// Một khoản bồi thường cụ thể trong dispute (ví dụ: hỏng ống kính, mất phụ kiện...).
		public sealed class AddItemRequest
		{
			public string Type { get; set; } = string.Empty;
			public decimal Amount { get; set; }
			public string? Notes { get; set; }
		}

		// Dùng khi BranchManager cập nhật trạng thái xử lý dispute.
		public sealed class UpdateStatusRequest
		{
			public string? ResolutionNote { get; set; }
		}

		[HttpGet("by-booking/{bookingId:guid}")]
		[SwaggerOperation(Summary = "Danh sách dispute của booking", Description = "Trả về các dispute mở liên quan tới một booking cụ thể. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<IEnumerable<DisputeDTO.DisputeResponse>>> GetByBooking(Guid bookingId)
		{
			var list = await _dispute.GetByBookingAsync(bookingId);
			return Ok(list);
		}

		[HttpGet("{id:guid}")]
		[SwaggerOperation(Summary = "Chi tiết dispute", Description = "Trả về thông tin chi tiết dispute theo id. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<DisputeDTO.DisputeResponse>> Get(Guid id)
		{
			var d = await _dispute.GetAsync(id);
			if (d == null) return NotFound();
			return Ok(d);
		}

		[HttpPost]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Staff mở dispute", Description = "Staff gửi dispute mới cho booking (mô tả, mức độ). Quyền: Staff")]
		public async Task<ActionResult<Guid>> Open([FromBody] OpenRequest req)
		{
			var id = await _dispute.OpenAsync(req.BookingId, req.Title, req.Description, req.Severity);
			return Ok(id);
		}

		[HttpPost("{id:guid}/items")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Thêm khoản bồi thường vào dispute", Description = "Staff thêm từng khoản (loại, số tiền, ghi chú) vào dispute. Quyền: Staff/BranchManager")]
		public async Task<IActionResult> AddItem(Guid id, [FromBody] AddItemRequest req)
		{
			await _dispute.AddItemAsync(id, req.Type, req.Amount, req.Notes);
			return NoContent();
		}

		[HttpPut("{id:guid}/resolved")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Cập nhật trạng thái dispute", Description = "Cập nhật trạng thái xử lý dispute (under_review/resolved/...).")]
		public async Task<IActionResult> Resolve(Guid id, [FromBody] UpdateStatusRequest req)
		{
			var status = "resolved";
			await _dispute.UpdateStatusAsync(id, status, req.ResolutionNote);
			return NoContent();
		}
		[HttpPut("{id:guid}/rejected")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Cập nhật trạng thái dispute", Description = "Cập nhật trạng thái xử lý dispute (under_review/resolved/...).")]
		public async Task<IActionResult> Reject(Guid id, [FromBody] UpdateStatusRequest req)
		{
			var status = "rejected";
			await _dispute.UpdateStatusAsync(id, status, req.ResolutionNote);
			return NoContent();
		}
	}
}

