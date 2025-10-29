using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class BookingController : ControllerBase
	{
		private readonly IBookingService _bookingService;
		public BookingController(IBookingService bookingService)
		{
			_bookingService = bookingService;
		}

		[HttpGet("{id:guid}")]
		public async Task<ActionResult<Booking>> GetById(Guid id)
		{
			var booking = await _bookingService.GetByIdAsync(id);
			if (booking == null) return NotFound();
			return Ok(booking);
		}

		public class CreateBookingRequest
		{
			public Guid RenterId { get; set; }
			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
		}

		[HttpPost]
		public async Task<ActionResult<Guid>> CreateDraft([FromBody] CreateBookingRequest request)
		{
			var id = await _bookingService.CreateDraftAsync(request.RenterId, request.PickupAt, request.ReturnAt);
			return CreatedAtAction(nameof(GetById), new { id }, id);
		}

		public class AddItemRequest
		{
			public Guid? CameraId { get; set; }
			public Guid? AccessoryId { get; set; }
			public int Quantity { get; set; }
			public decimal UnitPrice { get; set; }
			public decimal DepositAmount { get; set; }
		}

		[HttpPost("{id:guid}/items")]
		public async Task<IActionResult> AddItem(Guid id, [FromBody] AddItemRequest request)
		{
			await _bookingService.AddItemAsync(id, request.CameraId, request.AccessoryId, request.Quantity, request.UnitPrice, request.DepositAmount);
			return NoContent();
		}

		public class UpdateTimesRequest
		{
			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
		}

		[HttpPut("{id:guid}/times")]
		public async Task<IActionResult> UpdateTimes(Guid id, [FromBody] UpdateTimesRequest request)
		{
			await _bookingService.UpdateTimesAsync(id, request.PickupAt, request.ReturnAt);
			return NoContent();
		}

		[HttpPost("{id:guid}/submit")]
		public async Task<IActionResult> Submit(Guid id)
		{
			await _bookingService.SubmitForApprovalAsync(id);
			return NoContent();
		}

		[HttpPost("{id:guid}/approve")]
		public async Task<IActionResult> Approve(Guid id)
		{
			await _bookingService.ApproveAsync(id);
			return NoContent();
		}

		[HttpPost("{id:guid}/cancel")]
		public async Task<IActionResult> Cancel(Guid id)
		{
			await _bookingService.CancelAsync(id);
			return NoContent();
		}
	}
}
