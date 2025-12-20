using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.ContractDTO;
using static CamRent_Application.DTOs.InspectionDTO;

namespace CamRent_Application.DTOs
{
	public class BookingDTO
	{
		public class CreateBookingRequest
		{
			public Address Location { get; set; }
			[Required]
			public DateTime PickupAt { get; set; }
			[Required]
			public DateTime ReturnAt { get; set; }
		}
		public class BookingResponseDTO
		{
			public Guid Id { get; set; }
			public BookingType Type { get; set; } = BookingType.Rental;

			public Guid? RenterId { get; set; }
			public User? Renter { get; set; }
			public Guid? StaffId { get; set; }
			public string StaffName { get; set; }
			public Guid? BranchId { get; set; }
			public string BranchName { get; set; }
			public string BranchAddress { get; set; }
			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
			public Address? Location { get; set; }
			public DateTime CreatedAt { get; set; }
			public BookingStatus Status { get; set; }

			public string StatusText { get; set; } = "";

			// Snapshot pricing values for immutability
			public decimal SnapshotBaseDailyRate { get; set; }
			public decimal SnapshotPlatformFeePercent { get; set; }
			public decimal SnapshotRentalTotal { get; set; }
			public decimal SnapshotDepositAmount { get; set; }
			public ICollection<BookingItemDTO>? Items { get; set; }
			public ICollection<ContractResponse>? Contracts { get; set; }
			public ICollection<PaymentDTO>? Payments { get; set; }
		}
		public class Cart
		{
			public Guid Id { get; set; }
			public ICollection<BookingItemDTO> Items { get; set; } = new List<BookingItemDTO>();

		}

		public class BookingItemDTO
		{
			public Guid? ItemId { get; set; }
			public string? ItemName { get; set; }

			public string ItemType { get; set; }

			public decimal UnitPrice { get; set; }

			public decimal DepositAmount { get; set; }

			public List<FileAssetDTO>? Media { get; set; }

			public List<BookingItemUnavailableRangeDTO> UnavailableRanges { get; set; }

		}
		public class BookingItemUnavailableRangeDTO
		{
			public Guid BookingId { get; set; }
			public DateTime StartUtc { get; set; } // PickupAt (UTC)
			public DateTime EndUtc { get; set; }   // ReturnAt (UTC)
			public string Status { get; set; } = default!;
		}
		public class BookingStatusDTO
		{
			public BookingStatus Status { get; set; }
			public string StatusText { get; set; }
		}

		// QR payload cho booking, dùng để renter hiển thị QR và staff scan
		public class BookingQrDTO
		{
			public Guid BookingId { get; set; }
			// Chuỗi được encode vào QR (ví dụ: "booking:{GuidN}")
			public string Payload { get; set; } = string.Empty;
			// Ảnh QR dưới dạng byte[] (PNG), khi serialize JSON sẽ thành base64
			public byte[] PngImage { get; set; } = Array.Empty<byte>();
		}

		/// <summary>
		/// Tóm tắt một renter (người thuê) đã từng thuê thiết bị của một owner.
		/// Dùng cho màn Owner xem danh sách khách thuê thiết bị của mình.
		/// </summary>
		public class OwnerRenterSummaryDTO
		{
			public Guid RenterId { get; set; }
			public string RenterName { get; set; } = string.Empty;
			public string Email { get; set; } = string.Empty;
			public int TotalBookings { get; set; }
			public DateTime? LastPickupAt { get; set; }
		}

		/// <summary>
		/// Thông tin một booking cụ thể giữa owner và renter, chỉ chứa các items thuộc owner đó.
		/// </summary>
		public class OwnerRenterBookingDTO
		{
			public Guid BookingId { get; set; }
			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
			public BookingStatus Status { get; set; }
			public string StatusText { get; set; } = string.Empty;
			public List<OwnerRenterBookingItemDTO> Items { get; set; } = new();
		}

		/// <summary>
		/// Item trong booking thuộc về owner (camera / accessory).
		/// </summary>
		public class OwnerRenterBookingItemDTO
		{
			public Guid ItemId { get; set; }
			public string ItemName { get; set; } = string.Empty;
			public string ItemType { get; set; } = string.Empty; // "camera" / "accessory"
			public decimal UnitPrice { get; set; }
		}

		public class PaymentDTO
		{
			public Guid Id { get; set; }
			public Guid? BookingId { get; set; }
			public PaymentStatus Status { get; set; }
			public string Provider { get; set; } = "PayOS";
			public string Purpose { get; set; } = "booking";
			public string? ProviderPaymentId { get; set; }
			public decimal AuthorizedAmount { get; set; }
			public decimal CapturedAmount { get; set; }
			public decimal RefundedAmount { get; set; }
			public List<PaymentLineDTO> Lines { get; set; } = new();

		}
		public class PaymentLineDTO
		{
			public Guid PaymentId { get; set; }
			public string Type { get; set; } = string.Empty; // rental, deposit, delivery_fee, adjustment
			public decimal Amount { get; set; }
			public decimal CapturedAmount { get; set; }
			public decimal RefundedAmount { get; set; }
			public string Currency { get; set; } = "VND";
		}
	}
}
