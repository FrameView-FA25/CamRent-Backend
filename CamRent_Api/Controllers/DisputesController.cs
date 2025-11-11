using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class DisputesController : ControllerBase
	{
		private readonly IDisputeService _dispute;
		public DisputesController(IDisputeService dispute) { _dispute = dispute; }

		public sealed class OpenRequest { public Guid BookingId { get; set; } public string Title { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string Severity { get; set; } = "minor"; }
		public sealed class AddItemRequest { public string Type { get; set; } = string.Empty; public decimal Amount { get; set; } public string? Notes { get; set; } }
		public sealed class UpdateStatusRequest { public string Status { get; set; } = "under_review"; public string? ResolutionNote { get; set; } }

		[HttpGet("by-booking/{bookingId:guid}")]
		public async Task<ActionResult<IEnumerable<DisputeDTO.DisputeResponse>>> GetByBooking(Guid bookingId)
		{
			var list = await _dispute.GetByBookingAsync(bookingId);
			return Ok(list);
		}

		[HttpGet("{id:guid}")]
		public async Task<ActionResult<DisputeDTO.DisputeResponse>> Get(Guid id)
		{
			var d = await _dispute.GetAsync(id);
			if (d == null) return NotFound();
			return Ok(d);
		}

		[HttpPost]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<Guid>> Open([FromBody] OpenRequest req)
		{
			var id = await _dispute.OpenAsync(req.BookingId, req.Title, req.Description, req.Severity);
			return Ok(id);
		}

		[HttpPost("{id:guid}/items")]
		[Authorize(Policy = "Staff")]
		public async Task<IActionResult> AddItem(Guid id, [FromBody] AddItemRequest req)
		{
			await _dispute.AddItemAsync(id, req.Type, req.Amount, req.Notes);
			return NoContent();
		}

		[HttpPost("{id:guid}/status")]
		[Authorize(Policy = "BranchManager")]
		public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
		{
			await _dispute.UpdateStatusAsync(id, req.Status, req.ResolutionNote);
			return NoContent();
		}
	}
}

