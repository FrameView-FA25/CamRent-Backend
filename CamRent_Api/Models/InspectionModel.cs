using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class InspectionModel
	{
		public class CreateInspectionRequest
		{
			[Required]
			public Guid BookingId { get; set; }
			[Required]
			public InspectionType Type { get; set; }
			public Guid? PerformedByUserId { get; set; }
			public Guid? BranchId { get; set; }
			public string? Notes { get; set; }
			public List<Item> Items { get; set; } = new();
			public class Item { [Required] public string Section { get; set; } = string.Empty; [Required] public string Label { get; set; } = string.Empty; public string? Value { get; set; } public bool? Passed { get; set; } public string? Notes { get; set; } }
		}
	}
}
