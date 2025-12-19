using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Http;
using static CamRent_Application.DTOs.BookingReportDTO;

namespace CamRent_Application.Services
{
	public sealed class BookingReportService : IBookingReportService
	{
		private readonly IUnitOfWork _uow;
		private readonly IFileStorageService _files;

		public BookingReportService(IUnitOfWork uow, IFileStorageService files)
		{
			_uow = uow;
			_files = files;
		}

		public async Task<BookingReportResponse> CreateForRenterAsync(
			Guid renterUserId,
			Guid bookingId,
			string title,
			string description,
			string severity,
			IReadOnlyList<IFormFile>? images,
			CancellationToken ct = default)
		{
			if (bookingId == Guid.Empty) throw new AppException("BookingId không hợp lệ");
			if (string.IsNullOrWhiteSpace(title)) throw new AppException("Title là bắt buộc");
			if (string.IsNullOrWhiteSpace(description)) throw new AppException("Description là bắt buộc");

			var sev = string.IsNullOrWhiteSpace(severity) ? "minor" : severity.Trim().ToLowerInvariant();
			if (sev is not ("minor" or "major" or "critical"))
				throw new AppException("Severity chỉ nhận: minor | major | critical");

			var booking = await _uow.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new AppException("Booking không tồn tại");

			if (!booking.RenterId.HasValue || booking.RenterId.Value != renterUserId)
				throw new AppException("Bạn không có quyền report booking này");

			// Chỉ cho phép report khi đang thuê (đã nhận máy hoặc quá hạn)
			var allowedStatuses = new[] { BookingStatus.PickedUp, BookingStatus.Overdue };
			if (!allowedStatuses.Contains(booking.Status))
				throw new AppException("Chỉ có thể report khi booking đang ở trạng thái Đã nhận máy / Quá hạn");

			var report = new BookingIssueReport
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				ReporterUserId = renterUserId,
				Title = title.Trim(),
				Description = description.Trim(),
				Severity = sev,
				Status = "open",
				CreatedByUserId = renterUserId
			};

			await _uow.Repository<BookingIssueReport>().AddAsync(report);
			await _uow.Complete();

			var imageUrls = new List<string>();
			if (images != null && images.Count > 0)
			{
				if (images.Count > 8)
					throw new AppException("Tối đa 8 ảnh cho một report");

				foreach (var img in images.Where(f => f != null && f.Length > 0))
				{
					var asset = await _files.UploadAsync(
						img,
						ownerId: report.Id,
						ownerType: FileOwnerType.BookingReport,
						folder: $"camrent/bookings/{bookingId}/reports/{report.Id}",
						label: report.Title);
					imageUrls.Add(asset.Url);
				}
			}

			return new BookingReportResponse
			{
				Id = report.Id,
				BookingId = report.BookingId,
				ReporterUserId = report.ReporterUserId,
				Title = report.Title,
				Description = report.Description,
				Severity = report.Severity,
				Status = report.Status,
				CreatedAt = report.CreatedAt,
				ImageUrls = imageUrls
			};
		}
	}
}

