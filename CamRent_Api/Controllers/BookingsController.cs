using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.BookingModel;
using static CamRent_Application.DTOs.BookingDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class BookingsController : ControllerBase
	{

		private readonly IBookingService _bookingService;
		private readonly IPricingService _pricingService;
		public BookingsController(IBookingService bookingService, IPricingService pricingService)
		{
			_bookingService = bookingService;
			_pricingService = pricingService;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetAll()
		{
			var bookings = await _bookingService.GetAllAsync();
			return Ok(bookings);
		}

		[HttpGet("{id:guid}")]
		public async Task<ActionResult<Booking>> GetById(Guid id)
		{
			var booking = await _bookingService.GetByIdAsync(id);
			if (booking == null) return NotFound();
			return Ok(booking);
		}

		[HttpPost()]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult> CreateBooking([FromBody] CreateBookingRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var booking = await _bookingService.CreateBookingAsync(request, Guid.Parse(userId));
			if (booking == 0) return BadRequest("Tạo booking thất bại.");
			return Ok("Tạo đơn hàng thành công");

		}

		[HttpGet("renterbookings")]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetBookingByRenterId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			var bookings = await _bookingService.GetBookingsByRenterIdAsync(Guid.Parse(userId));
				return Ok(bookings);
		}

		[HttpGet("staffbookings")]
		[Authorize(Policy = "Staff")]
		public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetBookingByStaffId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var bookings = await _bookingService.GetBookingsByStaffIdAsync(Guid.Parse(userId));
			return Ok(bookings);
		}
		[HttpGet("GetBookingStatus")]
		public async Task<ActionResult<IEnumerable<BookingStatusDTO>>> GetBookingStatus()
		{
			var statuses = await _bookingService.GetBookingStatusesAsync();
			return Ok(statuses);
		}

		[HttpPut("{id:guid}/assign-staff/{staffId:guid}")]
		[Authorize(Policy = "BranchManager")]
		public async Task<IActionResult> AssignStaff(Guid id, Guid staffId)
		{
			var result = await _bookingService.AssignStaffToBookingsAsync(id, staffId);
			if(result > 0)
			{
				return NoContent();
			}
			return BadRequest();
		}

		[HttpGet("GetCard")]
		[Authorize(Policy = "Renter")]
		public async Task<ActionResult<Cart>> GetCard()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			var booking = await _bookingService.GetCartByRenterIdAsync(Guid.Parse(userId));
			if (booking == null) return NotFound();

			return Ok(booking);
		}

		[HttpPost("AddToCart")]
		[Authorize(Policy = "Renter")]
		public async Task<IActionResult> AddToCart([FromForm] AddToCartRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var result = await _bookingService.AddToCart(Guid.Parse(userId), request.Id, request.Type, request.Quantity);
			if (result > 0)
			{
				return Ok();
			}
			return BadRequest();
		}

		[HttpPost("RemoveFromCart")]
		[Authorize(Policy = "Renter")]
		public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var result = await _bookingService.RemoveFromCart(Guid.Parse(userId), request.Id, request.Type);
			if (result > 0)
			{
				return NoContent();
			}
			return BadRequest();
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
