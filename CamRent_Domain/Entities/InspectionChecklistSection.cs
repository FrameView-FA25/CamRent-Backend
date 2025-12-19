using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class InspectionChecklistSection : BaseEntity
	{
		public Guid TemplateId { get; set; }
		public InspectionChecklistTemplate Template { get; set; } = default!;

		public string Name { get; set; } = string.Empty;
		public int SortOrder { get; set; }

		public ICollection<InspectionChecklistItem> Items { get; set; } = new List<InspectionChecklistItem>();
	}
}

