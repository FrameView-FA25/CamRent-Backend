using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.BookingModel;
using static CamRent_Application.DTOs.BookingDTO;
using Swashbuckle.AspNetCore.Annotations;

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
		[SwaggerOperation(Summary = "Danh sách mọi booking", Description = "Trả về toàn bộ booking trong hệ thống. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetAll()
		{
			var bookings = await _bookingService.GetAllAsync();
			return Ok(bookings);
		}

		[HttpGet("{id:guid}")]
		[SwaggerOperation(Summary = "Chi tiết booking", Description = "Trả về booking theo id bao gồm items và snapshot giá. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var booking = await _bookingService.GetByIdAsync(id);
			if (booking == null) return NotFound();
			return Ok(booking);
		}

		// QR cho renter: dùng để hiển thị cho staff quét khi nhận/trả hàng
		[HttpGet("{id:guid}/qr")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "QR booking cho renter", Description = "Tạo QR cho booking của renter hiện tại để staff quét khi nhận/trả hàng. Quyền: Renter")]
		public async Task<IActionResult> GetBookingQr(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var qr = await _bookingService.GenerateBookingQrForRenterAsync(id, Guid.Parse(userId), HttpContext.RequestAborted);
			if (qr == null)
				return NotFound();

			return Ok(qr);
		}

		[HttpPost()]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Tạo booking từ giỏ hàng", Description = "Chuyển trạng thái giỏ hàng hiện tại của renter thành booking chờ duyệt. Quyền: Renter")]
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
		[SwaggerOperation(Summary = "Booking của renter", Description = "Danh sách booking (không bao gồm draft) của renter đang đăng nhập. Quyền: Renter")]
		public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetBookingByRenterId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			var bookings = await _bookingService.GetBookingsByRenterIdAsync(Guid.Parse(userId));
			return Ok(bookings);
		}

		[HttpGet("GetCard")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Lấy giỏ hiện tại", Description = "Trả về booking draft (giỏ hàng) của renter, bao gồm items và tổng tiền. Quyền: Renter")]
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
		[SwaggerOperation(Summary = "Thêm item vào giỏ", Description = "Thêm thiết bị/combo vào booking draft hiện tại của renter. Quyền: Renter")]
		public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var (success, message) = await _bookingService.AddToCart(Guid.Parse(userId), request.Id, request.Type);
			if (success)
			{
				return Ok();
			}
			return BadRequest(message ?? "Thêm vào giỏ hàng thất bại");
		}

		[HttpDelete("RemoveFromCart")]
		[Authorize(Policy = "Renter")]
		[SwaggerOperation(Summary = "Xóa item khỏi giỏ", Description = "Xóa thiết bị/combo khỏi booking draft hiện tại của renter. Quyền: Renter")]
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

		[HttpGet("branchbookings")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Booking theo chi nhánh", Description = "Danh sách booking mà branch manager chịu trách nhiệm (theo chi nhánh). Quyền: BranchManager")]
		public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetBookingByBranchId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var bookings = await _bookingService.GetBookingsByBranchManagerIdAsync(Guid.Parse(userId));
				return Ok(bookings);
		}
		

		
		[HttpGet("GetBookingStatus")]
		[SwaggerOperation(Summary = "Danh sách trạng thái booking", Description = "Trả về danh sách enum trạng thái booking và mô tả tiếng Việt. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<IEnumerable<BookingStatusDTO>>> GetBookingStatus()
		{
			var statuses = await _bookingService.GetBookingStatusesAsync();
			return Ok(statuses);
		}

		[HttpPut("{id:guid}/assign-staff/{staffId:guid}")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Gán nhân viên cho booking", Description = "Branch manager gán một staff cụ thể để chăm sóc booking. Quyền: BranchManager")]
		public async Task<IActionResult> AssignStaff(Guid id, Guid staffId)
		{
			var result = await _bookingService.AssignStaffToBookingsAsync(id, staffId);
			if(result > 0)
			{
				return NoContent();
			}
			return BadRequest();
		}
		[HttpGet("staffbookings")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Booking của staff", Description = "Danh sách booking mà nhân viên (staff) hiện tại được phân công xử lý. Quyền: Staff")]
		public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetBookingByStaffId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var bookings = await _bookingService.GetBookingsByStaffIdAsync(Guid.Parse(userId));
			return Ok(bookings);
		}
		[HttpPut("{id:guid}/update-status")]
		[Authorize(Policy = "ManagerOrStaff")]
		public async Task<IActionResult> UpdateBookingStatus(Guid id, BookingStatus status)
		{
			var result = await _bookingService.UpdateBookingStatusAsync(id, status);
			if (result > 0)
			{
				return NoContent();
			}
			return BadRequest();
		}

		[HttpGet("{id:guid}/quote")]
		[SwaggerOperation(Summary = "Tính quote giá thuê", Description = "Tính toán giá thuê, deposit, phí platform cho booking cụ thể. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<PricingQuoteResult>> Quote(Guid id, [FromQuery] decimal? platformFeePercent, [FromQuery] decimal ownerShareRatio = 0.75m)
		{
			var quote = await _pricingService.QuoteBookingAsync(id, platformFeePercent, ownerShareRatio);
			return Ok(quote);
		}


		[HttpPost("{id:guid}/settlement")]
		[SwaggerOperation(Summary = "Tính toán quyết toán cọc", Description = "Tính toán khoản khấu trừ/hoàn trả deposit dựa trên thông số bàn giao sau khi kết thúc booking. Quyền: Người dùng đã đăng nhập")]
		public async Task<ActionResult<DepositSettlement>> Settlement(Guid id, [FromBody] SettlementRequest request)
		{
			var result = await _pricingService.ComputeSettlementAsync(id, request.LateDays, request.RepairCost, request.DowntimeDays, request.MissingAccessoriesCost, request.CleaningCost);
			return Ok(result);
		}

		
	}
}
