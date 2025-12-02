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

	// Điểm dữ liệu theo thời gian cho biểu đồ (theo ngày / theo tháng)
	public class DashboardTimePoint
	{
		// Với thống kê theo ngày: Date = ngày (UTC)
		// Với thống kê theo tháng: Date = ngày đầu tiên của tháng (UTC)
		public DateTime Date { get; set; }

		// Số booking được tạo trong khoảng này
		public int BookingCount { get; set; }

		// Tổng tiền đã capture trong khoảng này
		public decimal CapturedRevenue { get; set; }
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

		// Thống kê theo thời gian (phục vụ vẽ biểu đồ)
		// Mặc định: 30 ngày gần nhất
		public List<DashboardTimePoint> DailyStats { get; set; } = new();

		// Mặc định: 12 tháng gần nhất
		public List<DashboardTimePoint> MonthlyStats { get; set; } = new();

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

		// Thống kê theo thời gian cho owner (dựa trên booking có thiết bị của owner)
		// 30 ngày gần nhất
		public List<DashboardTimePoint> DailyStats { get; set; } = new();

		// 12 tháng gần nhất
		public List<DashboardTimePoint> MonthlyStats { get; set; } = new();
	}

	// Dashboard dành cho Staff (nhân viên vận hành tại chi nhánh / platform)
	public class StaffDashboardDTO
	{
		// Booking được phân công cho staff này
		public int TotalAssignedBookings { get; set; }
		public List<BookingStatusCount> BookingsByStatus { get; set; } = new();

		// Công việc trong ngày
		public int TodayPickupBookings { get; set; }
		public int TodayReturnBookings { get; set; }

		// Nhiệm vụ hỗ trợ khác
		public int PendingVerificationRequests { get; set; }
		public int PendingReviewsToModerate { get; set; }
	}
}




