using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class VerificationRequest : BaseEntity
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
		public DateTime InspectionDate { get; set; }
		public string Status { get; set; } = "pending"; // pending, approved, rejected

        public User? Owner { get; set; }
		public Guid? StaffId { get; set; }
        public User? Staff { get; set; }
        
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string? Notes { get; set; }
        
        public ICollection<VerificationRequestItem> Items { get; set; } = new List<VerificationRequestItem>();
		public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    }

    public class VerificationRequestItem : BaseEntity
	{
        public Guid VerificationId { get; set; }
        public VerificationRequest? VerificationRequest { get; set; }
		public Guid? CameraId { get; set; }
		public Camera? Camera { get; set; }

		public Guid? AccessoryId { get; set; }
		public Accessory? Accessory { get; set; }
	}
}

