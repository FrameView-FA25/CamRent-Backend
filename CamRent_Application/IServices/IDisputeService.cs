using CamRent_Application.DTOs;

namespace CamRent_Application.IServices
{
	public interface IDisputeService
	{
		Task<IEnumerable<DisputeDTO.DisputeResponse>> GetByBookingAsync(Guid bookingId);
		Task<DisputeDTO.DisputeResponse?> GetAsync(Guid disputeId);
		Task<Guid> OpenAsync(Guid bookingId, string title, string description, string severity);
		Task AddItemAsync(Guid disputeId, string type, decimal amount, string? notes);
		Task AssignAsync(Guid disputeId, Guid? userId);
		Task UpdateStatusAsync(Guid disputeId, string status);
		Task<decimal> CalculateTotalDisputeAmountByBookingIdAsync(Guid bookingId);
	}
}

