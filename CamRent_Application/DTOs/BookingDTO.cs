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

			// Snapshot pricing values for immutability
			public decimal SnapshotBaseDailyRate { get; set; }
			public decimal SnapshotDepositPercent { get; set; }
			public decimal SnapshotPlatformFeePercent { get; set; }
			public decimal SnapshotRentalTotal { get; set; }
			public decimal SnapshotDepositAmount { get; set; }

			public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
		}
	}
}
