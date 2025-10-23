using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Booking : BaseEntity
    {
        public Guid? CameraId { get; set; }
        public Camera? Camera { get; set; }

        public Guid? AccessoryId { get; set; }
        public Accessory? Accessory { get; set; }

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
    }
}

