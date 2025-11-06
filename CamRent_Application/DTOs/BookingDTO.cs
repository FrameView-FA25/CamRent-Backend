using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class BookingDTO
	{
		public class BookingResponseDTO
		{
			public Guid Id { get; set; }
			public BookingType Type { get; set; } = BookingType.Rental;

			public Guid? RenterId { get; set; }
			public User? Renter { get; set; }

			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
			public BookingStatus Status { get; set; }

			public string StatusText { get; set; } = "";

			// Snapshot pricing values for immutability
			public decimal SnapshotBaseDailyRate { get; set; }
			public decimal SnapshotDepositPercent { get; set; }
			public decimal SnapshotPlatformFeePercent { get; set; }
			public decimal SnapshotRentalTotal { get; set; }
			public decimal SnapshotDepositAmount { get; set; }

			public ICollection<BookingItemDTO> Items { get; set; } = new List<BookingItemDTO>();
		}

		public class BookingItemDTO
		{
			public Guid? CameraId { get; set; }
			public string? CameraName { get; set; }

			public Guid? AccessoryId { get; set; }
			public string? AccessoryName { get; set; }

			public Guid? ComboId { get; set; }
			public string? ComboName { get; set; }

			public int Quantity { get; set; } = 1;
			public decimal UnitPrice { get; set; }
			public decimal DepositAmount { get; set; }
		}
	}
}
