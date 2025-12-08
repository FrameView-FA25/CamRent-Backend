using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Booking : BaseEntity
    {
        public string? BookingCode { get; set; } = string.Empty;
		public Guid? RenterId { get; set; }
        public User? Renter { get; set; }

        public Guid? StaffId { get; set; }
        public User? Staff { get; set; }
		public DateTime PickupAt { get; set; }
        public Address? Location { get; set; }
		public DateTime ReturnAt { get; set; }
		public Guid? BranchId { get; set; }
		public Branch? Branch { get; set; }
		public BookingStatus Status { get; set; }

        // Snapshot pricing values for immutability
        public decimal SnapshotBaseDailyRate { get; set; }
        public decimal SnapshotPlatformFeePercent { get; set; }
        public decimal SnapshotRentalTotal { get; set; }
        public decimal SnapshotDepositAmount { get; set; }

		public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
		public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
	}
}

