using Microsoft.AspNetCore.Http;
using static CamRent_Application.DTOs.BookingReportDTO;

namespace CamRent_Application.IServices
{
	public interface IBookingReportService
	{
		Task<BookingReportResponse> CreateForRenterAsync(
			Guid renterUserId,
			Guid bookingId,
			string title,
			string description,
			string severity,
			IReadOnlyList<IFormFile>? images,
			CancellationToken ct = default);
	}
}

