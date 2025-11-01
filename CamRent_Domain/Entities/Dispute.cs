using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Dispute : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "minor";
        public string Status { get; set; } = "open"; // open, under_review, resolved, rejected

        public ICollection<DisputeItem> Items { get; set; } = new List<DisputeItem>();
        public ICollection<FileAsset> Evidence { get; set; } = new List<FileAsset>();
    }

    public class DisputeItem : BaseEntity
    {
        public Guid DisputeId { get; set; }
        public Dispute Dispute { get; set; } = default!;
        public string Type { get; set; } = string.Empty; // damage, missing_accessory, late_fee, cleaning
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }
}

