using CamRent_Domain.Entities;

namespace CamRent_Application.IServices
{
	public interface IBookingService
	{
		Task<Booking?> GetByIdAsync(Guid bookingId);
		Task<Guid> CreateDraftAsync(Guid renterId, DateTime pickupAt, DateTime returnAt);
		Task AddItemAsync(Guid bookingId, Guid? cameraId, Guid? accessoryId, int quantity, decimal unitPrice, decimal depositAmount);
		Task RemoveItemAsync(Guid bookingItemId);
		Task UpdateTimesAsync(Guid bookingId, DateTime pickupAt, DateTime returnAt);
		Task SubmitForApprovalAsync(Guid bookingId);
		Task ApproveAsync(Guid bookingId);
		Task CancelAsync(Guid bookingId);
		Task<int> ProcessStatusesAsync(DateTime nowUtc);
	}
}
