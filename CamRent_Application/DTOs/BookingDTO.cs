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

			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
			public Address? Location { get; set; }
			public BookingStatus Status { get; set; }

			public string StatusText { get; set; } = "";

			// Snapshot pricing values for immutability
			public decimal SnapshotBaseDailyRate { get; set; }
			public decimal SnapshotDepositPercent { get; set; }
			public decimal SnapshotPlatformFeePercent { get; set; }
			public decimal SnapshotRentalTotal { get; set; }
			public decimal SnapshotDepositAmount { get; set; }

			public ICollection<BookingItemDTO>? Items { get; set; } 
			public ICollection<ContractResponse>? Contracts { get; set; } 
			public ICollection<InspectionResponseDTO>? Inspections { get; set; }
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
	}
}
