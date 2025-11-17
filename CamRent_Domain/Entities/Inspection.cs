using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace CamRent_Domain.Entities
{
    public class Inspection : BaseEntity
    {
        public InspectionType? Type { get; set; }
		public string Section { get; set; } = string.Empty;
		public string Label { get; set; } = string.Empty;
		public string? Value { get; set; }
        public bool? Passed { get; set; }
        public string Notes { get; set; } = string.Empty;
		public User? Staff { get; set; }
		public string? RenterSignatureUrl { get; set; }
		public string? StaffSignatureUrl { get; set; }
		public DateTime? PerformedAt { get; set; }
		public Guid? ManagerId { get; set; }
		public User? Manager { get; set; }
		public Guid? BranchId { get; set; }
		public Branch? Branch { get; set; }
		public Guid? ItemId { get; set; }
		public ItemType? ItemType { get; set; }
		public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; } = default!;
		public Guid? VerificationId { get; set; }
		public VerificationRequest? Verification { get; set; }

    }
}

