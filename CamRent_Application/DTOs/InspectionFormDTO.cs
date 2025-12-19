using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations;
using static CamRent_Application.DTOs.InspectionChecklistDTO;

namespace CamRent_Application.DTOs
{
	public class InspectionFormDTO
	{
		public class CreateInspectionFormRequest
		{
			[Required]
			public ItemType ItemType { get; set; }
			[Required]
			public Guid ItemId { get; set; }
			[Required]
			public InspectionType Type { get; set; }
			[Required]
			public Guid InspectionTypeId { get; set; }
			public HandoverType? HandoverType { get; set; }
			public Guid? BranchId { get; set; }
			public bool? Passed { get; set; }
			[MinLength(1)]
			public List<InspectionChecklistDTO.SubmitChecklistRowRequest> Rows { get; set; } = new();
		}

		public class UpdateInspectionFormRequest
		{
			public bool? Passed { get; set; }
			[MinLength(1)]
			public List<InspectionFormRowUpdateRequest> Rows { get; set; } = new();
		}

		public class InspectionFormRowUpdateRequest
		{
			[Required]
			public Guid InspectionId { get; set; }
			public List<Guid> MethodIds { get; set; } = new();
			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
		}

		public class InspectionFormResponse
		{
			public Guid Id { get; set; }
			public Guid TemplateId { get; set; }
			public string TemplateName { get; set; } = string.Empty;
			public Guid? StaffId { get; set; }
			public string? StaffName { get; set; }

			public ItemType ItemType { get; set; }
			public Guid ItemId { get; set; }
			public InspectionType Type { get; set; }
			public HandoverType? HandoverType { get; set; }
			public Guid InspectionTypeId { get; set; }
			public Guid? BranchId { get; set; }

			public bool? OverallPassed { get; set; }
			public DateTime CreatedAt { get; set; }

			public List<InspectionFormRowResponse> Rows { get; set; } = new();
		}

		public class InspectionFormSummaryResponse
		{
			public Guid Id { get; set; }
			public Guid TemplateId { get; set; }
			public string TemplateName { get; set; } = string.Empty;
			public Guid? StaffId { get; set; }
			public string? StaffName { get; set; }
			public ItemType ItemType { get; set; }
			public Guid ItemId { get; set; }
			public InspectionType Type { get; set; }
			public HandoverType? HandoverType { get; set; }
			public Guid InspectionTypeId { get; set; }
			public Guid? BranchId { get; set; }
			public bool? OverallPassed { get; set; }
			public DateTime CreatedAt { get; set; }
		}

		public class InspectionFormRowResponse
		{
			public Guid InspectionId { get; set; }
			public string Section { get; set; } = string.Empty;
			public string Label { get; set; } = string.Empty;
			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
			public List<InspectionMethodResponse> Methods { get; set; } = new();
			public List<FileAssetDTO> Media { get; set; } = new();
		}
	}
}
