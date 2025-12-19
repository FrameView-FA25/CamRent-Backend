using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class InspectionChecklistItemAllowedMethod : BaseEntity
	{
		public Guid ChecklistItemId { get; set; }
		public InspectionChecklistItem ChecklistItem { get; set; } = default!;

		public Guid MethodId { get; set; }
		public InspectionMethod Method { get; set; } = default!;
	}
}

