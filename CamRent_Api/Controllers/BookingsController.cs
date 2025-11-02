using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.BookingModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class BookingsController : ControllerBase
	{

		private readonly IBookingService _bookingService;
		private readonly IPricingService _pricingService;
		public BookingsController(IBookingService bookingService, IPricingService pricingService)
		{
			_bookingService = bookingService;
			_pricingService = pricingService;
		}

		[HttpGet("{id:guid}")]
		public async Task<ActionResult<Booking>> GetById(Guid id)
		{
			var booking = await _bookingService.GetByIdAsync(id);
			if (booking == null) return NotFound();
			return Ok(booking);
		}


		[HttpPost]
		public async Task<ActionResult<Guid>> CreateDraft([FromBody] CreateBookingRequest request)
		{
			var id = await _bookingService.CreateDraftAsync(request.RenterId, request.PickupAt, request.ReturnAt);
			return CreatedAtAction(nameof(GetById), new { id }, id);
		}



		[HttpPost("{id:guid}/items")]
		public async Task<IActionResult> AddItem(Guid id, [FromBody] AddItemRequest request)
		{
			await _bookingService.AddItemAsync(id, request.CameraId, request.AccessoryId, request.Quantity, request.UnitPrice, request.DepositAmount);
			return NoContent();
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

		[HttpGet("{id:guid}/quote")]
		public async Task<ActionResult<PricingQuoteResult>> Quote(Guid id, [FromQuery] decimal? platformFeePercent, [FromQuery] decimal ownerShareRatio = 0.75m)
		{
			var quote = await _pricingService.QuoteBookingAsync(id, platformFeePercent, ownerShareRatio);
			return Ok(quote);
		}


		[HttpPost("{id:guid}/settlement")]
		public async Task<ActionResult<DepositSettlement>> Settlement(Guid id, [FromBody] SettlementRequest request)
		{
			var result = await _pricingService.ComputeSettlementAsync(id, request.LateDays, request.RepairCost, request.DowntimeDays, request.MissingAccessoriesCost, request.CleaningCost);
			return Ok(result);
		}
	}
}
