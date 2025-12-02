using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.BookingDTO;
using System.Threading;

namespace CamRent_Application.IServices
{
	public interface IBookingService
	{
		Task<BookingResponseDTO?> GetByIdAsync(Guid bookingId);
		Task<List<BookingResponseDTO>> GetAllAsync();
		Task<int> CreateBookingAsync(CreateBookingRequest createBookingRequest, Guid renterId);
		Task<int> AssignStaffToBookingsAsync(Guid bookingId, Guid staffUserId);
		Task<List<BookingResponseDTO>> GetBookingsByRenterIdAsync(Guid renterId);
		Task<List<BookingResponseDTO>> GetBookingsByStaffIdAsync(Guid staffId);
		Task<List<BookingResponseDTO>> GetBookingsByBranchManagerIdAsync(Guid managerId);
		// Changed: return tuple with success flag and optional message
		Task<(bool Success, string? Message)> AddToCart(Guid renterId, Guid id, ItemType type);

		Task<int> RemoveFromCart(Guid renterId, Guid id, ItemType type);
		Task<Cart?> GetCartByRenterIdAsync(Guid renterId);
		Task<List<BookingStatusDTO>> GetBookingStatusesAsync();

		// Added: update booking status by id (returns >0 on success)
		Task<int> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);

		// QR cho booking để renter hiển thị cho staff scan
		Task<BookingQrDTO?> GenerateBookingQrForRenterAsync(Guid bookingId, Guid renterId, CancellationToken ct = default);
	}
}
