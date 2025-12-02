using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Application.Common;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CamRent_Application.Services
{
	public sealed class DashboardService : IDashboardService
	{
		private readonly IUnitOfWork _uow;

		public DashboardService(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public async Task<AdminDashboardDTO> GetAdminDashboardAsync(CancellationToken ct = default)
		{
			// Users & roles
			var users = await _uow.Repository<User>().ListAsync(include: q => q.Include(u => u.Roles));
			var totalUsers = users.Count();

			int CountByRole(UserRole role) => users.Count(u => u.Roles.Any(r => r.Role == role));

			var renters = CountByRole(UserRole.Renter);
			var owners = CountByRole(UserRole.Owner);
			var staffs = CountByRole(UserRole.Staff);
			var managers = CountByRole(UserRole.BranchManager);

			// Branch & inventory
			var totalBranches = (await _uow.Repository<Branch>().GetAllAsync()).Count;
			var totalCameras = (await _uow.Repository<Camera>().GetAllAsync()).Count;
			var totalAccessories = (await _uow.Repository<Accessory>().GetAllAsync()).Count;
			var totalCombos = (await _uow.Repository<Combo>().GetAllAsync()).Count;

			// Bookings
			var bookings = await _uow.Repository<Booking>().ListAsync();
			var totalBookings = bookings.Count();
			var bookingsByStatus = bookings
				.GroupBy(b => b.Status)
				.Select(g => new BookingStatusCount
				{
					Status = g.Key,
					StatusText = g.Key.GetDisplayName(),
					Count = g.Count()
				})
				.ToList();

			// Payments (revenue)
			var payments = await _uow.Repository<Payment>().ListAsync();
			var totalCaptured = payments.Sum(p => p.CapturedAmount);
			var totalRefunded = payments.Sum(p => p.RefundedAmount);

			// Thống kê theo thời gian
			// --- Theo ngày: 30 ngày gần nhất (theo CreatedAt UTC) ---
			var today = DateTime.UtcNow.Date;
			var fromDay = today.AddDays(-29); // gồm cả hôm nay => 30 ngày

			var dailyStats = bookings
				.Where(b => b.CreatedAt.Date >= fromDay && b.CreatedAt.Date <= today)
				.GroupBy(b => b.CreatedAt.Date)
				.Select(g =>
				{
					var date = g.Key;
					var dayRevenue = payments
						.Where(p => p.CreatedAt.Date == date)
						.Sum(p => p.CapturedAmount);
					return new DashboardTimePoint
					{
						Date = date,
						BookingCount = g.Count(),
						CapturedRevenue = dayRevenue
					};
				})
				.OrderBy(x => x.Date)
				.ToList();

			// --- Theo tháng: 12 tháng gần nhất ---
			var thisMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
			var fromMonthStart = thisMonthStart.AddMonths(-11);

			var monthlyStats = bookings
				.Where(b => b.CreatedAt >= fromMonthStart)
				.GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
				.Select(g =>
				{
					var monthStart = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc);
					var monthRevenue = payments
						.Where(p => p.CreatedAt.Year == g.Key.Year && p.CreatedAt.Month == g.Key.Month)
						.Sum(p => p.CapturedAmount);
					return new DashboardTimePoint
					{
						Date = monthStart,
						BookingCount = g.Count(),
						CapturedRevenue = monthRevenue
					};
				})
				.OrderBy(x => x.Date)
				.ToList();

			// Disputes
			var disputes = await _uow.Repository<Dispute>().ListAsync();
			var openDisputes = disputes.Count(d => d.Status == "open" || d.Status == "under_review");
			var resolvedDisputes = disputes.Count(d => d.Status == "resolved");

			return new AdminDashboardDTO
			{
				TotalUsers = totalUsers,
				TotalRenters = renters,
				TotalOwners = owners,
				TotalStaffs = staffs,
				TotalBranchManagers = managers,

				TotalBranches = totalBranches,
				TotalCameras = totalCameras,
				TotalAccessories = totalAccessories,
				TotalCombos = totalCombos,

				TotalBookings = totalBookings,
				BookingsByStatus = bookingsByStatus,

				TotalCapturedRevenue = totalCaptured,
				TotalRefundedAmount = totalRefunded,

				OpenDisputes = openDisputes,
				ResolvedDisputes = resolvedDisputes,

				DailyStats = dailyStats,
				MonthlyStats = monthlyStats
			};
		}

		public async Task<ManagerDashboardDTO> GetManagerDashboardAsync(Guid managerUserId, CancellationToken ct = default)
		{
			// Branch that this manager owns
			var branches = await _uow.Repository<Branch>()
				.ListAsync(b => b.ManagerId == managerUserId);
			var branch = branches.FirstOrDefault()
				?? throw new InvalidOperationException("Branch not found for this manager");

			var branchId = branch.Id;

			// Inventory in branch
			var camerasInBranch = (await _uow.Repository<Camera>()
				.ListAsync(c => c.BranchId == branchId)).Count();
			var accessoriesInBranch = (await _uow.Repository<Accessory>()
				.ListAsync(a => a.BranchId == branchId)).Count();

			// Bookings in branch
			var bookings = await _uow.Repository<Booking>()
				.ListAsync(b => b.BranchId == branchId);
			var totalBookings = bookings.Count();
			var bookingsByStatus = bookings
				.GroupBy(b => b.Status)
				.Select(g => new BookingStatusCount
				{
					Status = g.Key,
					StatusText = g.Key.GetDisplayName(),
					Count = g.Count()
				})
				.ToList();

			// Payments for bookings in this branch
			var bookingIds = bookings.Select(b => b.Id).ToHashSet();
			var payments = await _uow.Repository<Payment>().ListAsync(p => bookingIds.Contains(p.BookingId));
			var totalCaptured = payments.Sum(p => p.CapturedAmount);

			// Disputes for bookings in branch
			var disputes = await _uow.Repository<Dispute>().ListAsync(d => bookingIds.Contains(d.BookingId));
			var openDisputes = disputes.Count(d => d.Status == "open" || d.Status == "under_review");

			return new ManagerDashboardDTO
			{
				BranchId = branch.Id,
				BranchName = branch.Name,
				CamerasInBranch = camerasInBranch,
				AccessoriesInBranch = accessoriesInBranch,
				TotalBookings = totalBookings,
				BookingsByStatus = bookingsByStatus,
				TotalCapturedRevenue = totalCaptured,
				OpenDisputes = openDisputes
			};
		}

		public async Task<StaffDashboardDTO> GetStaffDashboardAsync(Guid staffUserId, CancellationToken ct = default)
		{
			// Booking được phân công cho staff này
			var bookings = await _uow.Repository<Booking>()
				.ListAsync(b => b.StaffId == staffUserId);

			var totalBookings = bookings.Count();
			var bookingsByStatus = bookings
				.GroupBy(b => b.Status)
				.Select(g => new BookingStatusCount
				{
					Status = g.Key,
					StatusText = g.Key.GetDisplayName(),
					Count = g.Count()
				})
				.ToList();

			var today = DateTime.UtcNow.Date;
			var todayPickups = bookings.Count(b => b.PickupAt.Date == today);
			var todayReturns = bookings.Count(b => b.ReturnAt.Date == today);

			// Verification requests do staff này phụ trách
			var verifications = await _uow.Repository<VerificationRequest>()
				.ListAsync(v => v.StaffId == staffUserId);
			var pendingVerifs = verifications.Count(v => v.Status == VerificationStatus.Pending);

			// Reviews do staff này moderates
			var reviews = await _uow.Repository<Review>()
				.ListAsync(r => r.ReviewedByStaffId == staffUserId);
			var pendingReviews = reviews.Count(r => r.Status == ReviewStatus.Pending);

			return new StaffDashboardDTO
			{
				TotalAssignedBookings = totalBookings,
				BookingsByStatus = bookingsByStatus,
				TodayPickupBookings = todayPickups,
				TodayReturnBookings = todayReturns,
				PendingVerificationRequests = pendingVerifs,
				PendingReviewsToModerate = pendingReviews
			};
		}

		public async Task<OwnerDashboardDTO> GetOwnerDashboardAsync(Guid ownerUserId, CancellationToken ct = default)
		{
			// Lấy thiết bị của owner
			var cameras = await _uow.Repository<Camera>().ListAsync(c => c.OwnerUserId == ownerUserId);
			var accessories = await _uow.Repository<Accessory>().ListAsync(a => a.OwnerUserId == ownerUserId);

			var cameraIds = cameras.Select(c => c.Id).ToHashSet();
			var accessoryIds = accessories.Select(a => a.Id).ToHashSet();

			// Booking items liên quan tới thiết bị của owner
			var bookingItems = await _uow.Repository<BookingItem>()
				.ListAsync(bi =>
					(cameraIds.Contains(bi.CameraId ?? Guid.Empty)) ||
					(accessoryIds.Contains(bi.AccessoryId ?? Guid.Empty)),
					include: q => q.Include(bi => bi.Booking));

			// Chỉ tính các booking hợp lệ (không canceled/no-show)
			var validStatuses = new[]
			{
				BookingStatus.Confirmed,
				BookingStatus.PickedUp,
				BookingStatus.Returned,
				BookingStatus.Completed,
				BookingStatus.Overdue
			};

			var validItems = bookingItems
				.Where(bi => bi.Booking != null && validStatuses.Contains(bi.Booking.Status))
				.ToList();

			// Tổng booking distinct
			var totalBookingsForOwnerItems = validItems
				.Where(bi => bi.Booking != null)
				.Select(bi => bi.Booking!.Id)
				.Distinct()
				.Count();

			// Doanh thu gộp ước tính: UnitPrice * days
			decimal totalRevenue = 0;
			var assetStats = new Dictionary<(Guid id, string type, string name), OwnerAssetStat>();
			// Lưu doanh thu theo booking để vẽ biểu đồ theo thời gian
			var bookingRevenue = new Dictionary<Guid, decimal>();

			foreach (var item in validItems)
			{
				if (item.Booking == null) continue;

				var booking = item.Booking;
				var days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));
				var gross = item.UnitPrice * days;

				totalRevenue += gross;

				// Dồn doanh thu theo booking
				if (!bookingRevenue.TryGetValue(booking.Id, out var current))
					current = 0;
				bookingRevenue[booking.Id] = current + gross;

				string type;
				string name;
				Guid assetId;

				if (item.CameraId.HasValue)
				{
					type = "camera";
					assetId = item.CameraId.Value;
					var cam = cameras.FirstOrDefault(c => c.Id == assetId);
					name = cam != null ? $"{cam.Brand} {cam.Model}" : "Camera";
				}
				else if (item.AccessoryId.HasValue)
				{
					type = "accessory";
					assetId = item.AccessoryId.Value;
					var acc = accessories.FirstOrDefault(a => a.Id == assetId);
					name = acc != null ? $"{acc.Brand} {acc.Model}" : "Accessory";
				}
				else
				{
					continue;
				}

				var key = (assetId, type, name);
				if (!assetStats.TryGetValue(key, out var stat))
				{
					stat = new OwnerAssetStat
					{
						ItemId = assetId,
						ItemType = type,
						Name = name,
						RentalCount = 0,
						GrossRevenue = 0
					};
					assetStats[key] = stat;
				}

				stat.RentalCount += 1;
				stat.GrossRevenue += gross;
			}

			// Chuẩn bị dữ liệu booking + doanh thu theo booking
			var bookingInfo = validItems
				.Where(bi => bi.Booking != null)
				.GroupBy(bi => bi.Booking!.Id)
				.Select(g => new
				{
					Booking = g.First().Booking!,
					Gross = bookingRevenue.TryGetValue(g.Key, out var gr) ? gr : 0
				})
				.ToList();

			// Thống kê theo ngày: 30 ngày gần nhất (dựa trên PickupAt)
			var today = DateTime.UtcNow.Date;
			var fromDay = today.AddDays(-29);

			var dailyStats = bookingInfo
				.Where(x => x.Booking.PickupAt.Date >= fromDay && x.Booking.PickupAt.Date <= today)
				.GroupBy(x => x.Booking.PickupAt.Date)
				.Select(g => new DashboardTimePoint
				{
					Date = g.Key,
					BookingCount = g.Count(),
					CapturedRevenue = g.Sum(x => x.Gross)
				})
				.OrderBy(x => x.Date)
				.ToList();

			// Thống kê theo tháng: 12 tháng gần nhất (dựa trên PickupAt)
			var thisMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
			var fromMonthStart = thisMonthStart.AddMonths(-11);

			var monthlyStats = bookingInfo
				.Where(x => x.Booking.PickupAt >= fromMonthStart)
				.GroupBy(x => new { x.Booking.PickupAt.Year, x.Booking.PickupAt.Month })
				.Select(g =>
				{
					var monthStart = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc);
					return new DashboardTimePoint
					{
						Date = monthStart,
						BookingCount = g.Count(),
						CapturedRevenue = g.Sum(x => x.Gross)
					};
				})
				.OrderBy(x => x.Date)
				.ToList();

			var topAssets = assetStats.Values
				.OrderByDescending(a => a.RentalCount)
				.ThenByDescending(a => a.GrossRevenue)
				.Take(10)
				.ToList();

			return new OwnerDashboardDTO
			{
				TotalCameras = cameras.Count(),
				TotalAccessories = accessories.Count(),
				TotalBookingsForOwnerItems = totalBookingsForOwnerItems,
				TotalGrossRevenue = totalRevenue,
				TopRentedAssets = topAssets,
				DailyStats = dailyStats,
				MonthlyStats = monthlyStats
			};
		}
	}
}




