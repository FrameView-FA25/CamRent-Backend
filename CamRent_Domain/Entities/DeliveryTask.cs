using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class DeliveryTask : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public Guid? AssigneeUserId { get; set; }
        public User? AssigneeUser { get; set; }

        public DeliveryTaskStatus Status { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? TrackingCode { get; set; }
        public string? Notes { get; set; }
        public Address? PickupAddress { get; set; }
        public Address? DropoffAddress { get; set; }
        public decimal? DeliveryFee { get; set; }
    }
}

