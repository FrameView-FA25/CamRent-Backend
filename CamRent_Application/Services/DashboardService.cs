using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
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
			var totalUsers = users.Count;

			int CountByRole(UserRole role) => users.Count(u => u.Roles.Any(r => r.Role == role));

			var renters = CountByRole(UserRole.Renter);
			var owners = CountByRole(UserRole.Owner);
			var staffs = CountByRole(UserRole.Staff);
			var managers = CountByRole(UserRole.BranchManager);

			// Branch & inventory
			var totalBranches = await _uow.Repository<Branch>().CountAsync();
			var totalCameras = await _uow.Repository<Camera>().CountAsync();
			var totalAccessories = await _uow.Repository<Accessory>().CountAsync();
			var totalCombos = await _uow.Repository<Combo>().CountAsync();

			// Bookings
			var bookings = await _uow.Repository<Booking>().ListAsync();
			var totalBookings = bookings.Count;
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
				ResolvedDisputes = resolvedDisputes
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
			var camerasInBranch = await _uow.Repository<Camera>().CountAsync(c => c.BranchId == branchId);
			var accessoriesInBranch = await _uow.Repository<Accessory>().CountAsync(a => a.BranchId == branchId);

			// Bookings in branch
			var bookings = await _uow.Repository<Booking>()
				.ListAsync(b => b.BranchId == branchId);
			var totalBookings = bookings.Count;
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
				BookingStatus.InUse,
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

			foreach (var item in validItems)
			{
				if (item.Booking == null) continue;

				var days = Math.Max(1, (int)Math.Ceiling((item.Booking.ReturnAt - item.Booking.PickupAt).TotalDays));
				var gross = item.UnitPrice * days;

				totalRevenue += gross;

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

			var topAssets = assetStats.Values
				.OrderByDescending(a => a.RentalCount)
				.ThenByDescending(a => a.GrossRevenue)
				.Take(10)
				.ToList();

			return new OwnerDashboardDTO
			{
				TotalCameras = cameras.Count,
				TotalAccessories = accessories.Count,
				TotalBookingsForOwnerItems = totalBookingsForOwnerItems,
				TotalGrossRevenue = totalRevenue,
				TopRentedAssets = topAssets
			};
		}
	}
}




