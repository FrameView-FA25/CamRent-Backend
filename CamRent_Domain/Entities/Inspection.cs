using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace CamRent_Domain.Entities
{
	public class Inspection : BaseEntity
    {
		public Guid? FormId { get; set; }
		public InspectionForm? Form { get; set; }
		public Guid? ChecklistItemId { get; set; }
		public string Section { get; set; } = string.Empty;
		public string Label { get; set; } = string.Empty;
		public string? Value { get; set; }
        public bool? Passed { get; set; }
        public string Notes { get; set; } = string.Empty;

		public ICollection<InspectionMethodSelection> MethodSelections { get; set; } = new List<InspectionMethodSelection>();

    }
}
