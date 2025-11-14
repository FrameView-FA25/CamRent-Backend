using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class VerificationRequest : BaseEntity
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
		public DateTime InspectionDate { get; set; }
		public string Status { get; set; } = "pending"; // pending, approved, rejected

		public Guid? TargetUserId { get; set; }
        public User? TargetUser { get; set; }
        
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string? Notes { get; set; }
        
        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    }
}

