using System;
using System.Collections.Generic;
using CamRent_Domain.Common;

namespace CamRent_Application.DTOs
{
	/// <summary>
	/// Số lượng booking theo từng trạng thái, dùng chung cho nhiều loại dashboard.
	/// </summary>
	public class BookingStatusCount
	{
		public BookingStatus Status { get; set; }
		public string StatusText { get; set; } = string.Empty;
		public int Count { get; set; }
	}

	/// <summary>
	/// Một điểm dữ liệu trên biểu đồ thời gian (theo ngày/tháng) cho dashboard:
	/// thể hiện số booking và doanh thu capture được tại mốc thời gian đó.
	/// </summary>
	public class DashboardTimePoint
	{
		public DateTime Date { get; set; }
		public int BookingCount { get; set; }
		public decimal CapturedRevenue { get; set; }
	}

	/// <summary>
	/// Dữ liệu dashboard cho Admin toàn hệ thống:
	/// tổng quan user, chi nhánh, inventory, booking, doanh thu và dispute.
	/// </summary>
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

	/// <summary>
	/// Dữ liệu dashboard cho BranchManager:
	/// tập trung vào một chi nhánh cụ thể (inventory, booking, doanh thu, dispute).
	/// </summary>
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

	/// <summary>
	/// Thống kê theo từng thiết bị (camera/phụ kiện) của owner:
	/// số lần được thuê và doanh thu gộp tạo ra.
	/// </summary>
	public class OwnerAssetStat
	{
		public Guid ItemId { get; set; }
		public string ItemType { get; set; } = string.Empty; // camera / accessory
		public string Name { get; set; } = string.Empty;
		public int RentalCount { get; set; }
		public decimal GrossRevenue { get; set; }
	}

	/// <summary>
	/// Dữ liệu dashboard cho Owner (chủ thiết bị).
	/// </summary>
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

	/// <summary>
	/// Dữ liệu dashboard cho Staff:
	/// số lượng booking, verification và review mà staff cần xử lý.
	/// </summary>
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
