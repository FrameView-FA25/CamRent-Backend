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

	public class DashboardTimePoint
	{
		public DateTime Date { get; set; }
		public int BookingCount { get; set; }
		public decimal CapturedRevenue { get; set; }
	}

	public class AdminDashboardDTO
	{
		public int TotalUsers { get; set; }
		public int TotalRenters { get; set; }
		public int TotalOwners { get; set; }
		public int TotalStaffs { get; set; }
		public int TotalBranchManagers { get; set; }

		public int TotalBranches { get; set; }
		public int TotalCameras { get; set; }
		public int TotalAccessories { get; set; }
		public int TotalCombos { get; set; }

		public int TotalBookings { get; set; }
		public List<BookingStatusCount> BookingsByStatus { get; set; } = new();
		public decimal TotalCapturedRevenue { get; set; }
		public decimal TotalRefundedAmount { get; set; }

		public List<DashboardTimePoint> DailyStats { get; set; } = new();
		public List<DashboardTimePoint> MonthlyStats { get; set; } = new();

		public int OpenDisputes { get; set; }
		public int ResolvedDisputes { get; set; }
	}

	public class ManagerDashboardDTO
	{
		public Guid BranchId { get; set; }
		public string BranchName { get; set; } = string.Empty;
		public int CamerasInBranch { get; set; }
		public int AccessoriesInBranch { get; set; }
		public int TotalBookings { get; set; }
		public List<BookingStatusCount> BookingsByStatus { get; set; } = new();
		public decimal TotalCapturedRevenue { get; set; }
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
		public int TotalBookingsForOwnerItems { get; set; }
		public decimal TotalGrossRevenue { get; set; }
		public List<OwnerAssetStat> TopRentedAssets { get; set; } = new();
		public List<DashboardTimePoint> DailyStats { get; set; } = new();
		public List<DashboardTimePoint> MonthlyStats { get; set; } = new();
	}

	public class StaffDashboardDTO
	{
		public int TotalAssignedBookings { get; set; }
		public List<BookingStatusCount> BookingsByStatus { get; set; } = new();
		public int TodayPickupBookings { get; set; }
		public int TodayReturnBookings { get; set; }
		public int PendingVerificationRequests { get; set; }
		public int PendingReviewsToModerate { get; set; }
	}

	// Event lịch làm việc của staff (booking, verification, ...) cho manager xem
	public class StaffScheduleItemDTO
	{
		public Guid StaffId { get; set; }
		public string StaffName { get; set; } = string.Empty;
		public string EventType { get; set; } = string.Empty; // BookingPickup, BookingReturn, Verification
		public Guid? BookingId { get; set; }
		public Guid? VerificationId { get; set; }
		public DateTime StartAt { get; set; }
		public DateTime EndAt { get; set; }
		public string? Title { get; set; }
	}

	// Workload theo staff trong 1 khoảng thời gian cho BranchManager
	public class StaffWorkloadItemDTO
	{
		public Guid StaffId { get; set; }
		public string StaffName { get; set; } = string.Empty;
		public int AssignedBookings { get; set; }
		public int AssignedVerifications { get; set; }
		public int TodayPickupBookings { get; set; }
		public int TodayReturnBookings { get; set; }
	}

	public class StaffWorkloadSummaryDTO
	{
		public Guid BranchId { get; set; }
		public string BranchName { get; set; } = string.Empty;
		public List<StaffWorkloadItemDTO> Staffs { get; set; } = new();
	}
}
