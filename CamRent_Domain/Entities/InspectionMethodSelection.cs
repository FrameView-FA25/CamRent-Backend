using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class InspectionMethodSelection : BaseEntity
	{
		public Guid InspectionId { get; set; }
		public Inspection Inspection { get; set; } = default!;

		public Guid MethodId { get; set; }
		public InspectionMethod Method { get; set; } = default!;
	}
}

