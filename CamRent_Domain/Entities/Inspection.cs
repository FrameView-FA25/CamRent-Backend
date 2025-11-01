using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Inspection : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public InspectionType Type { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime? PerformedAt { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public User? PerformedByUser { get; set; }
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }
        public string? ChecklistTemplateVersion { get; set; }
        public Guid VerifyRequestId { get; set; }
        public VerificationRequest VerifyRequest { get; set; }
        public ICollection<InspectionItem> Items { get; set; } = new List<InspectionItem>();
        public ICollection<FileAsset> Media { get; set; } = new List<FileAsset>();
        public string? RenterSignatureUrl { get; set; }
        public string? StaffSignatureUrl { get; set; }
    }

    public class InspectionItem : BaseEntity
    {
        public Guid InspectionId { get; set; }
        public Inspection Inspection { get; set; } = default!;
        public string Section { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool? Passed { get; set; }
        public string? Notes { get; set; }
    }
}

