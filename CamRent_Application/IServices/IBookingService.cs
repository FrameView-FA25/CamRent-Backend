using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.BookingDTO;

namespace CamRent_Application.IServices
{
	public interface IBookingService
	{
		Task<Booking?> GetByIdAsync(Guid bookingId);
		Task FinalizeAsync(Guid bookingId, decimal ownerShareRatio, Guid platformUserId);
		Task<List<BookingResponseDTO>> GetAllAsync();
		Task<int> AssignStaffToBookingsAsync(Guid bookingId, Guid staffUserId);
		Task<List<BookingResponseDTO>> GetBookingsByRenterIdAsync(Guid renterId);
		Task<List<BookingResponseDTO>> GetBookingsByStaffIdAsync(Guid staffId);

		Task<int> AddToCart(Guid renterId, Guid id, ItemType type, int quantity);

		Task<int> RemoveFromCart(Guid renterId, Guid id, ItemType type);
		Task<Cart?> GetCartByRenterIdAsync(Guid renterId);
		Task<List<BookingStatusDTO>> GetBookingStatusesAsync();
	}
}
