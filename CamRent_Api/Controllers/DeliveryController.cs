using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class DeliveryController : ControllerBase
	{
		private readonly IDeliveryService _deliveryService;
		public DeliveryController(IDeliveryService deliveryService)
		{
			_deliveryService = deliveryService;
		}

		public class CreateTaskRequest { public Guid BookingId { get; set; } public Guid? AssigneeUserId { get; set; } public string? TrackingCode { get; set; } public string? Notes { get; set; } public decimal? DeliveryFee { get; set; } }
		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateTaskRequest request)
		{
			var id = await _deliveryService.CreateTaskAsync(request.BookingId, request.AssigneeUserId, request.TrackingCode, request.Notes, request.DeliveryFee);
			return Ok(id);
		}

		public class UpdateStatusRequest { public DeliveryTaskStatus Status { get; set; } public DateTime? WhenUtc { get; set; } }
		[HttpPost("{id:guid}/status")]
		public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
		{
			await _deliveryService.UpdateStatusAsync(id, request.Status, request.WhenUtc);
			return NoContent();
		}
	}
}
