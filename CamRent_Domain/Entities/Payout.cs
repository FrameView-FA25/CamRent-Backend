using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Payout : BaseEntity
    {
        public Guid OwnerUserId { get; set; }
        public User OwnerUser { get; set; } = default!;

        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = "pending"; // pending, paid, failed
        public DateTime? PaidAt { get; set; }
        public string? Reference { get; set; }
    }
}

