using CamRent_Domain.Bookings;
using CamRent_Domain.Common;
using CamRent_Domain.Users;

namespace CamRent_Domain.Delivery
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
    }
}

