using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class VerificationRequest : BaseEntity
    {
        public string Type { get; set; } = string.Empty; // user_kyc, device_verification
        public string Status { get; set; } = "pending"; // pending, approved, rejected

        public Guid? TargetUserId { get; set; }
        public User? TargetUser { get; set; }
        
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string? Notes { get; set; }
        
        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    }
}

