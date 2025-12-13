using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using static CamRent_Application.DTOs.BookingDTO;

namespace CamRent_Application.IServices
{
	public interface IBookingService
	{
		Task<BookingResponseDTO?> GetByIdAsync(Guid? bookingId);
		Task<List<BookingResponseDTO>> GetAllAsync();
		Task<Guid> CreateBookingAsync(CreateBookingRequest createBookingRequest, Guid renterId);
		Task<int> AssignStaffToBookingsAsync(Guid bookingId, Guid staffUserId);
		Task<List<BookingResponseDTO>> GetBookingsByRenterIdAsync(Guid renterId);
		Task<List<BookingResponseDTO>> GetBookingsByStaffIdAsync(Guid staffId);
		Task<List<BookingResponseDTO>> GetBookingsByBranchManagerIdAsync(Guid managerId);
		Task<(bool Success, string? Message)> AddToCart(Guid renterId, Guid id, ItemType type);
		Task<int> RemoveFromCart(Guid renterId, Guid id, ItemType type);
		Task<Cart?> GetCartByRenterIdAsync(Guid renterId);
		Task<List<BookingStatusDTO>> GetBookingStatusesAsync();
		Task<int> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);
		Task<BookingQrDTO?> GenerateBookingQrForRenterAsync(Guid bookingId, Guid renterId, CancellationToken ct = default);

		// Lịch bận của một thiết bị (camera/phụ kiện/combo) để hiển thị calendar tránh trùng lịch
		Task<List<BookingItemUnavailableRangeDTO>> GetUnavailableRangesForItemAsync(Guid itemId, ItemType type, CancellationToken cancellationToken = default);
	}
}
