using System;
using System.Collections.Generic;
using CamRent_Domain.Common;

namespace CamRent_Application.DTOs
{
	public class BookingStatusCount
	{
		public BookingStatus Status { get; set; }
		public string StatusText { get; set; } = string.Empty;
		public int Count { get; set; }
	}

	public class AdminDashboardDTO
	{
		// Users
		public int TotalUsers { get; set; }
		public int TotalRenters { get; set; }
		public int TotalOwners { get; set; }
		public int TotalStaffs { get; set; }
		public int TotalBranchManagers { get; set; }

		// Branches & inventory
		public int TotalBranches { get; set; }
		public int TotalCameras { get; set; }
		public int TotalAccessories { get; set; }
		public int TotalCombos { get; set; }

		// Bookings & payments
		public int TotalBookings { get; set; }
		public List<BookingStatusCount> BookingsByStatus { get; set; } = new();
		public decimal TotalCapturedRevenue { get; set; }
		public decimal TotalRefundedAmount { get; set; }

		// Disputes
		public int OpenDisputes { get; set; }
		public int ResolvedDisputes { get; set; }
	}

	public class ManagerDashboardDTO
	{
		public Guid BranchId { get; set; }
		public string BranchName { get; set; } = string.Empty;

		// Inventory in branch
		public int CamerasInBranch { get; set; }
		public int AccessoriesInBranch { get; set; }

		// Bookings in branch
		public int TotalBookings { get; set; }
		public List<BookingStatusCount> BookingsByStatus { get; set; } = new();

		// Payments for bookings in branch
		public decimal TotalCapturedRevenue { get; set; }

		// Disputes related to branch bookings
		public int OpenDisputes { get; set; }
	}

	public class OwnerAssetStat
	{
		public Guid ItemId { get; set; }
		public string ItemType { get; set; } = string.Empty; // camera / accessory
		public string Name { get; set; } = string.Empty;
		public int RentalCount { get; set; }
		public decimal GrossRevenue { get; set; }
	}

	public class OwnerDashboardDTO
	{
		public int TotalCameras { get; set; }
		public int TotalAccessories { get; set; }

		// Tổng số booking có chứa thiết bị của owner
		public int TotalBookingsForOwnerItems { get; set; }

		// Doanh thu gộp (chưa trừ platform fee) ước tính từ UnitPrice * days
		public decimal TotalGrossRevenue { get; set; }

		// Top thiết bị được thuê nhiều nhất
		public List<OwnerAssetStat> TopRentedAssets { get; set; } = new();
	}
}




