using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.BookingDTO;

namespace CamRent_Application.IServices
{
	public interface IBookingService
	{
		Task<Booking?> GetByIdAsync(Guid bookingId);
		Task<Guid> CreateDraftAsync(Guid renterId, DateTime pickupAt, DateTime returnAt);
		Task AddItemAsync(Guid bookingId, Guid? cameraId, Guid? accessoryId, Guid? comboId, int quantity, decimal unitPrice, decimal depositAmount);
		Task RemoveItemAsync(Guid bookingItemId);
		Task UpdateTimesAsync(Guid bookingId, DateTime pickupAt, DateTime returnAt);
		Task SubmitForApprovalAsync(Guid bookingId);
		Task ApproveAsync(Guid bookingId);
		Task CancelAsync(Guid bookingId);
		Task<int> ProcessStatusesAsync(DateTime nowUtc);
		Task FinalizeAsync(Guid bookingId, decimal ownerShareRatio, Guid platformUserId);

		Task<List<BookingResponseDTO>> GetAllAsync();
	}
}
