using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class InspectionMethod : BaseEntity
	{
		// Stable key for FE/BE mapping, e.g. "physical", "function"
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public int SortOrder { get; set; }
		public bool IsActive { get; set; } = true;
	}
}

