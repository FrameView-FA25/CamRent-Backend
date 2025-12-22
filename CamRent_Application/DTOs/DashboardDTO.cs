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
		/// <summary>
		/// Doanh thu gộp ước tính của chi nhánh (dựa trên BookingItem.UnitPrice * số ngày).
		/// </summary>
		public decimal TotalGrossRevenue { get; set; }
		/// <summary>
		/// Top thiết bị được thuê nhiều nhất trong chi nhánh.
		/// </summary>
		public List<OwnerAssetStat> TopRentedAssets { get; set; } = new();
		/// <summary>
		/// Biểu đồ theo ngày (30 ngày gần nhất) dựa trên PickupAt.
		/// </summary>
		public List<DashboardTimePoint> DailyStats { get; set; } = new();
		/// <summary>
		/// Biểu đồ theo tháng (12 tháng gần nhất) dựa trên PickupAt.
		/// </summary>
		public List<DashboardTimePoint> MonthlyStats { get; set; } = new();
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

	/// <summary>
	/// Thông tin availability của staff trong một khoảng thời gian cụ thể,
	/// phục vụ cho BranchManager chọn người để gán booking/verification mới.
	/// </summary>
	public class AvailableStaffItemDTO
	{
		public Guid StaffId { get; set; }
		public string StaffName { get; set; } = string.Empty;
		public bool IsAvailable { get; set; }
		public int ConflictingBookings { get; set; }
		public int ConflictingVerifications { get; set; }
		public int TodayPickupBookings { get; set; }
		public int TodayReturnBookings { get; set; }
	}

	public class AvailableStaffSummaryDTO
	{
		public Guid BranchId { get; set; }
		public string BranchName { get; set; } = string.Empty;
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public List<AvailableStaffItemDTO> Staffs { get; set; } = new();
	}

	/// <summary>
	/// Kết quả check availability của một staff trong một slot cụ thể.
	/// Dùng khi Manager/Staff click vào slot để tạo booking/verification mới.
	/// </summary>
	public class StaffSlotAvailabilityDTO
	{
		public Guid StaffId { get; set; }
		public string StaffName { get; set; } = string.Empty;
		public DateTime Date { get; set; }
		public int SlotIndex { get; set; }
		/// <summary>
		/// Loại công việc muốn gán: \"booking\" hoặc \"verification\".
		/// </summary>
		public string Type { get; set; } = "booking";
		public bool CanAssign { get; set; }
		public int ExistingBookings { get; set; }
		public int ExistingVerifications { get; set; }
		public List<StaffSlotBookingBriefDTO> Bookings { get; set; } = new();
		public List<StaffSlotVerificationBriefDTO> Verifications { get; set; } = new();
	}

	/// <summary>
	/// Thông tin ngắn gọn về booking nằm trong slot (dùng cho popup UI).
	/// </summary>
	public class StaffSlotBookingBriefDTO
	{
		public Guid BookingId { get; set; }
		public DateTime PickupAt { get; set; }
		public DateTime ReturnAt { get; set; }
		public BookingStatus Status { get; set; }
		public string StatusText { get; set; } = string.Empty;
		public Guid? RenterId { get; set; }
		public string? RenterName { get; set; }
	}

	/// <summary>
	/// Thông tin ngắn gọn về verification nằm trong slot (dùng cho popup UI).
	/// </summary>
	public class StaffSlotVerificationBriefDTO
	{
		public Guid VerificationId { get; set; }
		public DateTime InspectionDate { get; set; }
		public VerificationStatus Status { get; set; }
		public Guid? OwnerId { get; set; }
		public string? OwnerName { get; set; }
	}
}
