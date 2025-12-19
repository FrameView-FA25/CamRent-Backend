using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class InspectionChecklistTemplate : BaseEntity
	{
		public string Name { get; set; } = string.Empty;
		public ItemType ItemType { get; set; }

		// Optional: different templates per workflow (Booking/Verification)
		public InspectionType? InspectionType { get; set; }

		// Optional: branch-specific template
		public Guid? BranchId { get; set; }
		public Branch? Branch { get; set; }

		public bool IsActive { get; set; } = true;

		public ICollection<InspectionChecklistSection> Sections { get; set; } = new List<InspectionChecklistSection>();
	}
}

