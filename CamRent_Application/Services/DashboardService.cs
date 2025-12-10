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
	/// <summary>
	/// Cung cấp các thống kê tổng quan cho nhiều loại người dùng (Admin, BranchManager, Staff, Owner)
	/// dựa trên dữ liệu booking, payment, dispute, verification, v.v...
	/// </summary>
	public sealed class DashboardService : IDashboardService
	{
		private readonly IUnitOfWork _uow;

		public DashboardService(IUnitOfWork uow)
		{
			_uow = uow;
		}

		/// <summary>
		/// Dashboard tổng quan cho Admin toàn hệ thống:
		/// - Thống kê số lượng user theo từng role
		/// - Thống kê số lượng chi nhánh, thiết bị, booking
		/// - Tổng doanh thu thu được / số tiền đã refund
		/// - Biểu đồ booking + doanh thu theo ngày (30 ngày gần nhất) và theo tháng (12 tháng gần nhất)
		/// - Thống kê số lượng dispute đang mở / đã xử lý.
		/// </summary>
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

		/// <summary>
		/// Dashboard cho BranchManager:
		/// - Thống kê tồn kho (camera/phụ kiện) trong chi nhánh mà manager phụ trách
		/// - Booking, doanh thu và dispute liên quan tới chi nhánh đó.
		/// </summary>
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
			var payments = await _uow.Repository<Payment>().ListAsync(p => bookingIds.Contains((Guid)p.BookingId));
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

		/// <summary>
		/// Dashboard cho Staff:
		/// - Số lượng booking được gán cho staff, phân nhóm theo trạng thái
		/// - Số booking pickup/return trong ngày hôm nay
		/// - Số verification request và review đang pending mà staff cần xử lý.
		/// </summary>
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

		/// <summary>
		/// Dashboard cho Owner:
		/// - Thống kê tổng số camera/phụ kiện thuộc owner
		/// - Số booking distinct có sử dụng thiết bị của owner (bỏ qua booking cancel/no-show)
		/// - Tổng doanh thu gộp (ước tính) theo từng thiết bị
		/// - Top thiết bị được thuê nhiều nhất
		/// - Biểu đồ doanh thu/booking theo ngày và theo tháng dựa trên PickupAt.
		/// </summary>
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

			// Gộp doanh thu theo từng item/booking để:
			// - tính tổng doanh thu của owner
			// - thống kê theo từng thiết bị (assetStats)
			// - chuẩn bị dữ liệu vẽ biểu đồ doanh thu theo thời gian (bookingRevenue)
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

		/// <summary>
		/// Lịch làm việc của một staff: các booking được phân công (pickup/return) và các verification có InspectionDate
		/// trong khoảng thời gian from-to (nếu null thì lấy mặc định 30 ngày quanh hôm nay).
		/// </summary>
		public async Task<IReadOnlyList<StaffScheduleItemDTO>> GetStaffScheduleAsync(Guid staffUserId, DateTime? from, DateTime? to, CancellationToken ct = default)
		{
			var today = DateTime.UtcNow.Date;
			var fromDate = from?.Date ?? today.AddDays(-7);
			var toDate = to?.Date ?? today.AddDays(21);

			var result = new List<StaffScheduleItemDTO>();

			// Booking được gán cho staff
			var bookings = await _uow.Repository<Booking>().ListAsync(b => b.StaffId == staffUserId);

			foreach (var b in bookings)
			{
				// Sự kiện nhận máy (pickup)
				if (b.PickupAt.Date >= fromDate && b.PickupAt.Date <= toDate)
				{
					result.Add(new StaffScheduleItemDTO
					{
						StaffId = staffUserId,
						StaffName = b.Staff?.FullName ?? string.Empty,
						EventType = "BookingPickup",
						BookingId = b.Id,
						StartAt = b.PickupAt,
						EndAt = b.PickupAt,
						Title = $"Nhận máy booking {b.Id}"
					});
				}

				// Sự kiện trả máy (return)
				if (b.ReturnAt.Date >= fromDate && b.ReturnAt.Date <= toDate)
				{
					result.Add(new StaffScheduleItemDTO
					{
						StaffId = staffUserId,
						StaffName = b.Staff?.FullName ?? string.Empty,
						EventType = "BookingReturn",
						BookingId = b.Id,
						StartAt = b.ReturnAt,
						EndAt = b.ReturnAt,
						Title = $"Trả máy booking {b.Id}"
					});
				}
			}

			// Verification mà staff phụ trách
			var verifs = await _uow.Repository<VerificationRequest>()
				.ListAsync(v => v.StaffId == staffUserId);

			foreach (var v in verifs)
			{
				if (v.InspectionDate.Date >= fromDate && v.InspectionDate.Date <= toDate)
				{
					result.Add(new StaffScheduleItemDTO
					{
						StaffId = staffUserId,
						StaffName = v.Staff?.FullName ?? string.Empty,
						EventType = "Verification",
						VerificationId = v.Id,
						StartAt = v.InspectionDate,
						EndAt = v.InspectionDate,
						Title = $"Kiểm tra thiết bị verification {v.Id}"
					});
				}
			}

			return result
				.OrderBy(e => e.StartAt)
				.ToList();
		}

		/// <summary>
		/// Workload của tất cả staff trong chi nhánh của manager: số booking + verification được giao,
		/// cùng số lượng pickup/return trong ngày hiện tại.
		/// </summary>
		public async Task<StaffWorkloadSummaryDTO> GetStaffWorkloadForManagerAsync(Guid managerUserId, DateTime? from, DateTime? to, CancellationToken ct = default)
		{
			var branches = await _uow.Repository<Branch>().ListAsync(b => b.ManagerId == managerUserId);
			var branch = branches.FirstOrDefault()
				?? throw new InvalidOperationException("Branch not found for this manager");

			var branchId = branch.Id;
			var today = DateTime.UtcNow.Date;
			var fromDate = from?.Date ?? today.AddDays(-7);
			var toDate = to?.Date ?? today.AddDays(21);

			// Staff trong chi nhánh này = những user có booking hoặc verification thuộc branch
			var bookings = await _uow.Repository<Booking>()
				.ListAsync(b => b.BranchId == branchId && b.StaffId != null);
			var verifs = await _uow.Repository<VerificationRequest>()
				.ListAsync(v => v.BranchId == branchId && v.StaffId != null);

			var staffIds = bookings.Select(b => b.StaffId!.Value)
				.Concat(verifs.Select(v => v.StaffId!.Value))
				.Distinct()
				.ToList();

			var staffLookup = (await _uow.Repository<User>().ListAsync(u => staffIds.Contains(u.Id)))
				.ToDictionary(u => u.Id, u => u.FullName);

			var items = new List<StaffWorkloadItemDTO>();

			foreach (var staffId in staffIds)
			{
				var staffBookings = bookings.Where(b => b.StaffId == staffId).ToList();
				var staffVerifs = verifs.Where(v => v.StaffId == staffId).ToList();

				var assignedBookingsInRange = staffBookings
					.Count(b => b.PickupAt.Date <= toDate && b.ReturnAt.Date >= fromDate);

				var assignedVerifsInRange = staffVerifs
					.Count(v => v.InspectionDate.Date >= fromDate && v.InspectionDate.Date <= toDate);

				var todayPickups = staffBookings.Count(b => b.PickupAt.Date == today);
				var todayReturns = staffBookings.Count(b => b.ReturnAt.Date == today);

				items.Add(new StaffWorkloadItemDTO
				{
					StaffId = staffId,
					StaffName = staffLookup.TryGetValue(staffId, out var name) ? name : string.Empty,
					AssignedBookings = assignedBookingsInRange,
					AssignedVerifications = assignedVerifsInRange,
					TodayPickupBookings = todayPickups,
					TodayReturnBookings = todayReturns
				});
			}

			return new StaffWorkloadSummaryDTO
			{
				BranchId = branchId,
				BranchName = branch.Name,
				Staffs = items
			};
		}
	}
}




