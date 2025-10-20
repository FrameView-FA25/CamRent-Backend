using CamRent_Domain.Common;
using CamRent_Domain.Devices;
using CamRent_Domain.Users;

namespace CamRent_Domain.Bookings
{
    public class Booking : BaseEntity
    {
        public Guid DeviceId { get; set; }
        public Device Device { get; set; } = default!;

        public Guid RenterId { get; set; }
        public User Renter { get; set; } = default!;

        public DateTime PickupAt { get; set; }
        public DateTime ReturnAt { get; set; }
        public BookingStatus Status { get; set; }

        // Snapshot pricing values for immutability
        public decimal SnapshotBaseDailyRate { get; set; }
        public decimal SnapshotDepositPercent { get; set; }
        public decimal SnapshotPlatformFeePercent { get; set; }
        public decimal SnapshotRentalTotal { get; set; }
        public decimal SnapshotDepositAmount { get; set; }
    }
}

