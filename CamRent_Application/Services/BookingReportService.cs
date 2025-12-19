using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

		public async Task<IReadOnlyList<BookingIssueReportStaffListItem>> GetReportsForStaffAsync(
			Guid staffUserId,
			string? status,
			int limit,
			CancellationToken ct = default)
		{
			var branchId = await GetBranchIdForStaffAsync(staffUserId, ct);
			var take = limit <= 0 ? 20 : Math.Min(limit, 200);
			var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();

			var reports = await _uow.Repository<BookingIssueReport>().ListAsync(
				filter: r =>
					r.Booking.BranchId == branchId &&
					(normalizedStatus == null || r.Status.ToLower() == normalizedStatus),
				include: q => q
					.Include(r => r.Booking)
						.ThenInclude(b => b.Items!)
							.ThenInclude(i => i.Camera)
					.Include(r => r.Booking)
						.ThenInclude(b => b.Items!)
							.ThenInclude(i => i.Accessory)
					.Include(r => r.Booking)
						.ThenInclude(b => b.Items!)
							.ThenInclude(i => i.Combo)
					.Include(r => r.ReporterUser)
			);

			return reports
				.OrderByDescending(r => r.CreatedAt)
				.Take(take)
				.Select(r => new BookingIssueReportStaffListItem
				{
					Id = r.Id,
					BookingId = r.BookingId,
					BookingCode = r.Booking?.BookingCode,
					CreatedAt = r.CreatedAt,
					Title = r.Title,
					Severity = r.Severity,
					Status = r.Status,
					ReporterName = r.ReporterUser?.FullName ?? string.Empty,
					Devices = BuildDevicesFromBooking(r.Booking).ToList()
				})
				.ToList();
		}

		public async Task<BookingIssueReportStaffDetail?> GetReportDetailForStaffAsync(
			Guid staffUserId,
			Guid reportId,
			CancellationToken ct = default)
		{
			var branchId = await GetBranchIdForStaffAsync(staffUserId, ct);

			var list = await _uow.Repository<BookingIssueReport>().ListAsync(
				filter: r => r.Id == reportId && r.Booking.BranchId == branchId,
				include: q => q
					.Include(r => r.Booking)
						.ThenInclude(b => b.Items!)
							.ThenInclude(i => i.Camera)
					.Include(r => r.Booking)
						.ThenInclude(b => b.Items!)
							.ThenInclude(i => i.Accessory)
					.Include(r => r.Booking)
						.ThenInclude(b => b.Items!)
							.ThenInclude(i => i.Combo)
					.Include(r => r.ReporterUser)
			);

			var r = list.FirstOrDefault();
			if (r == null) return null;

			var files = await _uow.Repository<FileAsset>().ListAsync(
				f => f.OwnerType == FileOwnerType.BookingReport && f.OwnerId == r.Id);
			var imageUrls = files.Select(f => f.Url).Where(u => !string.IsNullOrWhiteSpace(u)).ToList();

			return new BookingIssueReportStaffDetail
			{
				Id = r.Id,
				BookingId = r.BookingId,
				BookingCode = r.Booking?.BookingCode,
				CreatedAt = r.CreatedAt,
				Title = r.Title,
				Description = r.Description,
				Severity = r.Severity,
				Status = r.Status,
				ReporterUserId = r.ReporterUserId,
				ReporterName = r.ReporterUser?.FullName ?? string.Empty,
				Devices = BuildDevicesFromBooking(r.Booking).ToList(),
				ImageUrls = imageUrls
			};
		}

		private async Task<Guid> GetBranchIdForStaffAsync(Guid staffUserId, CancellationToken ct = default)
		{
			var memberships = await _uow.Repository<UserBranchMembership>()
				.ListAsync(m => m.UserId == staffUserId);
			var branchId = memberships.Select(m => m.BranchId).FirstOrDefault();
			if (branchId == Guid.Empty)
				throw new AppException("Staff chưa được gán vào chi nhánh");
			return branchId;
		}

		private static IEnumerable<BookingReportDeviceBrief> BuildDevicesFromBooking(Booking? booking)
		{
			if (booking?.Items == null) yield break;

			foreach (var item in booking.Items)
			{
				if (item.CameraId.HasValue)
				{
					var cam = item.Camera;
					yield return new BookingReportDeviceBrief
					{
						ItemType = "camera",
						ItemId = item.CameraId.Value,
						Name = cam != null ? $"{cam.Brand} {cam.Model}" : "Camera",
						SerialNumber = cam?.SerialNumber
					};
				}
				else if (item.AccessoryId.HasValue)
				{
					var acc = item.Accessory;
					yield return new BookingReportDeviceBrief
					{
						ItemType = "accessory",
						ItemId = item.AccessoryId.Value,
						Name = acc != null ? $"{acc.Brand} {acc.Model}" : "Accessory",
						SerialNumber = acc?.SerialNumber
					};
				}
				else if (item.ComboId.HasValue)
				{
					var combo = item.Combo;
					yield return new BookingReportDeviceBrief
					{
						ItemType = "combo",
						ItemId = item.ComboId.Value,
						Name = combo?.Name ?? "Combo",
						SerialNumber = null
					};
				}
			}
		}
	}
}

