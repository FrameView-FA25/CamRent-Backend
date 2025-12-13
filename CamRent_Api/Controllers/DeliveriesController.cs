using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
		[SwaggerOperation(Summary = "Tạo nhiệm vụ giao nhận", Description = "Branch manager tạo một delivery task cho booking, gán nhân sự giao nhận, tracking code và khoản phí. Quyền: BranchManager")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateTaskRequest request)
		{
			var id = await _deliveryService.CreateTaskAsync(request.BookingId, request.AssigneeUserId, request.TrackingCode, request.Notes, request.DeliveryFee);
			return Ok(id);
		}

		[HttpPost("{id:guid}/status")]
		[Authorize(Policy = "Delivery")]
		[SwaggerOperation(Summary = "Cập nhật trạng thái giao nhận", Description = "Nhân sự giao nhận cập nhật trạng thái task (Assigned/InTransit/Delivered/...). Quyền: Delivery/Staff được phân quyền")]
		public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
		{
			await _deliveryService.UpdateStatusAsync(id, request.Status, request.WhenUtc);
			return NoContent();
		}
	}
}


