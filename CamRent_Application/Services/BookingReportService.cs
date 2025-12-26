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
				Status = "pending",
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
					try
					{
						var asset = await _files.UploadAsync(
							img,
							ownerId: report.Id,
							ownerType: FileOwnerType.BookingReport,
							folder: $"camrent/bookings/{bookingId}/reports/{report.Id}",
							label: report.Title);
						
						if (!string.IsNullOrWhiteSpace(asset?.Url))
						{
							imageUrls.Add(asset.Url);
						}
					}
					catch (Exception ex)
					{
						// Log lỗi nhưng tiếp tục với các ảnh khác
						// Nếu tất cả ảnh đều lỗi, vẫn trả về report nhưng ImageUrls sẽ rỗng
						// Frontend có thể hiển thị thông báo lỗi upload ảnh
						// TODO: Log error để debug: $"Failed to upload image {img.FileName}: {ex.Message}"
					}
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
			var branchId = await GetBranchIdForActorAsync(staffUserId, ct);
			var take = limit <= 0 ? 20 : Math.Min(limit, 200);
			var normalizedStatus = NormalizeStatus(status);

			var reports = await _uow.Repository<BookingIssueReport>().ListAsync(
				filter: r =>
					r.Booking.BranchId == branchId &&
					(normalizedStatus == null || r.Status == normalizedStatus),
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

			var reportList = reports
				.OrderByDescending(r => r.CreatedAt)
				.Take(take)
				.ToList();

			// Load hình ảnh cho tất cả reports
			var reportIds = reportList.Select(r => r.Id).ToList();
			var allFiles = await _uow.Repository<FileAsset>().ListAsync(
				f => f.OwnerType == FileOwnerType.BookingReport && f.OwnerId.HasValue && reportIds.Contains(f.OwnerId.Value));
			
			var filesByReportId = allFiles
				.Where(f => f.OwnerId.HasValue)
				.GroupBy(f => f.OwnerId!.Value)
				.ToDictionary(g => g.Key, g => g.Select(f => f.Url).Where(u => !string.IsNullOrWhiteSpace(u)).ToList());

			return reportList
				.Select(r => new BookingIssueReportStaffListItem
				{
					Id = r.Id,
					BookingId = r.BookingId,
					BookingCode = r.Booking?.BookingCode,
					CreatedAt = r.CreatedAt,
					Title = r.Title,
					Severity = r.Severity,
					Status = r.Status,
					StatusText = StatusText(r.Status),
					ReporterName = r.ReporterUser?.FullName ?? string.Empty,
					Devices = BuildDevicesFromBooking(r.Booking).ToList(),
					ImageUrls = filesByReportId.GetValueOrDefault(r.Id, new List<string>())
				})
				.ToList();
		}

		public async Task<BookingIssueReportStaffDetail?> GetReportDetailForStaffAsync(
			Guid staffUserId,
			Guid reportId,
			CancellationToken ct = default)
		{
			var branchId = await GetBranchIdForActorAsync(staffUserId, ct);

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
				StatusText = StatusText(r.Status),
				ReporterUserId = r.ReporterUserId,
				ReporterName = r.ReporterUser?.FullName ?? string.Empty,
				Devices = BuildDevicesFromBooking(r.Booking).ToList(),
				ImageUrls = imageUrls
			};
		}

		public async Task<bool> UpdateStatusForManagerAsync(
			Guid managerUserId,
			Guid reportId,
			string status,
			string? handlerNote,
			CancellationToken ct = default)
		{
			var branchId = await GetBranchIdForActorAsync(managerUserId, ct);
			var newStatus = NormalizeStatus(status);
			if (newStatus == null)
				throw new AppException("Status không hợp lệ");

			// Chỉ cho phép set các trạng thái xử lý cơ bản từ phía manager
			if (newStatus is not ("under_review" or "resolved" or "rejected" or "pending"))
				throw new AppException("Status chỉ nhận: pending | under_review | resolved | rejected");

			var list = await _uow.Repository<BookingIssueReport>().ListAsync(
				filter: r => r.Id == reportId && r.Booking.BranchId == branchId,
				include: q => q.Include(r => r.Booking));

			var report = list.FirstOrDefault();
			if (report == null) return false;

			report.Status = newStatus;
			report.HandledByStaffId = managerUserId;
			report.HandledAt = DateTime.UtcNow;
			report.HandlerNote = string.IsNullOrWhiteSpace(handlerNote) ? null : handlerNote.Trim();
			report.UpdatedByUserId = managerUserId;
			report.UpdatedAt = DateTime.UtcNow;

			await _uow.Repository<BookingIssueReport>().UpdateAsync(report);
			await _uow.Complete();
			return true;
		}

		private async Task<Guid> GetBranchIdForActorAsync(Guid userId, CancellationToken ct = default)
		{
			// 1) Nếu user là BranchManager (hoặc dữ liệu không có membership) => lấy theo Branch.ManagerId
			var managedBranch = (await _uow.Repository<Branch>().ListAsync(b => b.ManagerId == userId)).FirstOrDefault();
			if (managedBranch != null)
				return managedBranch.Id;

			// 2) Staff => lấy theo membership
			var memberships = await _uow.Repository<UserBranchMembership>()
				.ListAsync(m => m.UserId == userId);
			var branchId = memberships.Select(m => m.BranchId).FirstOrDefault();
			if (branchId == Guid.Empty)
				throw new AppException("Người dùng chưa được gán vào chi nhánh");
			return branchId;
		}

		private static string? NormalizeStatus(string? status)
		{
			if (string.IsNullOrWhiteSpace(status)) return null;
			var s = status.Trim().ToLowerInvariant();
			// Backward-compat: trước đây dùng "open" -> nay chuẩn là "pending"
			if (s == "open") s = "pending";
			return s;
		}

		private static string StatusText(string? status)
		{
			var s = (status ?? string.Empty).Trim().ToLowerInvariant();
			return s switch
			{
				"pending" => "Chờ xử lý",
				"under_review" => "Đang xử lý",
				"resolved" => "Đã xử lý",
				"rejected" => "Từ chối",
				_ => s
			};
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

