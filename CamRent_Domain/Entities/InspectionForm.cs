using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	// Represents one completed/ongoing checklist "form" (batch) created by staff
	public class InspectionForm : BaseEntity
	{
		public Guid TemplateId { get; set; }
		public InspectionChecklistTemplate Template { get; set; } = default!;

		public ItemType ItemType { get; set; }
		public Guid ItemId { get; set; }
		public InspectionType Type { get; set; }
		public Guid InspectionTypeId { get; set; } // BookingId or VerificationId
		public HandoverType? HandoverType { get; set; }
		public Guid? BranchId { get; set; }

		public bool? OverallPassed { get; set; }

		// Staff who created/performed this form (FK uses BaseEntity.CreatedByUserId)
		public User? Staff { get; set; }

		public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
	}
}
