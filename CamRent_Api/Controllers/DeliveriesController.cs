using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.DeliveryModel;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DeliveriesController : ControllerBase
	{
		private readonly IDeliveryService _deliveryService;
		public DeliveriesController(IDeliveryService deliveryService)
		{
			_deliveryService = deliveryService;
		}

		[HttpPost]
		[Authorize(Policy = "BranchManager")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateTaskRequest request)
		{
			var id = await _deliveryService.CreateTaskAsync(request.BookingId, request.AssigneeUserId, request.TrackingCode, request.Notes, request.DeliveryFee);
			return Ok(id);
		}

		[HttpPost("{id:guid}/status")]
		[Authorize(Policy = "Delivery")]
		public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
		{
			await _deliveryService.UpdateStatusAsync(id, request.Status, request.WhenUtc);
			return NoContent();
		}
	}
}


