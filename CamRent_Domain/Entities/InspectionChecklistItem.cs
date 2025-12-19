using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class InspectionChecklistItem : BaseEntity
	{
		public Guid SectionId { get; set; }
		public InspectionChecklistSection Section { get; set; } = default!;

		public string Label { get; set; } = string.Empty;
		public int SortOrder { get; set; }

		// Allowed methods/columns for this checklist row
		public ICollection<InspectionChecklistItemAllowedMethod> AllowedMethods { get; set; } = new List<InspectionChecklistItemAllowedMethod>();
	}
}
