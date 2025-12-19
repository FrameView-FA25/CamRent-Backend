using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CamRent_Application.DTOs
{
	public class InspectionChecklistDTO
	{
		public class ChecklistTemplateResponse
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = string.Empty;
			public ItemType ItemType { get; set; }
			public InspectionType? InspectionType { get; set; }
			public Guid? BranchId { get; set; }
			public bool IsActive { get; set; }
			public List<ChecklistSectionResponse> Sections { get; set; } = new();
		}

		public class ChecklistSectionResponse
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = string.Empty;
			public int SortOrder { get; set; }
			public List<ChecklistItemResponse> Items { get; set; } = new();
		}

		public class ChecklistItemResponse
		{
			public Guid Id { get; set; }
			public string Label { get; set; } = string.Empty;
			public int SortOrder { get; set; }
			public List<InspectionMethodResponse> AllowedMethods { get; set; } = new();
		}

		public class InspectionMethodResponse
		{
			public Guid Id { get; set; }
			public string Code { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
			public int SortOrder { get; set; }
			public bool IsActive { get; set; }
		}

		public class ChecklistTemplateSummaryResponse
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = string.Empty;
			public ItemType ItemType { get; set; }
			public InspectionType? InspectionType { get; set; }
			public Guid? BranchId { get; set; }
			public bool IsActive { get; set; }
			public DateTime CreatedAt { get; set; }
			public DateTime? UpdatedAt { get; set; }
		}

		public class UpsertChecklistTemplateRequest
		{
			[Required]
			public string Name { get; set; } = string.Empty;
			[Required]
			public ItemType ItemType { get; set; }
			public InspectionType? InspectionType { get; set; }
			public Guid? BranchId { get; set; }
			public bool IsActive { get; set; } = true;
			public List<UpsertChecklistSectionRequest> Sections { get; set; } = new();
		}

		public class UpsertChecklistSectionRequest
		{
			[Required]
			public string Name { get; set; } = string.Empty;
			public int SortOrder { get; set; }
			public List<UpsertChecklistItemRequest> Items { get; set; } = new();
		}

		public class UpsertChecklistItemRequest
		{
			[Required]
			public string Label { get; set; } = string.Empty;
			public int SortOrder { get; set; }

			// Use MethodIds to bind methods to this checklist row
			public List<Guid> AllowedMethodIds { get; set; } = new();
		}

		// Admin CRUD for methods
		public class UpsertInspectionMethodRequest
		{
			[Required]
			public string Code { get; set; } = string.Empty;
			[Required]
			public string Name { get; set; } = string.Empty;
			public int SortOrder { get; set; }
			public bool IsActive { get; set; } = true;
		}

		// Staff submits checklist results in a single request (no files here)
		public class SubmitChecklistResultRequest
		{
			[Required]
			public ItemType ItemType { get; set; }
			[Required]
			public Guid ItemId { get; set; }
			[Required]
			public InspectionType Type { get; set; }
			public HandoverType? HandoverType { get; set; }
			[Required]
			public Guid InspectionTypeId { get; set; } // BookingId or VerifyRequestId

			public Guid? BranchId { get; set; }

			// Optional: force overall result; otherwise derived from rows
			public bool? Passed { get; set; }

			[MinLength(1, ErrorMessage = "At least one checklist row is required.")]
			public List<SubmitChecklistRowRequest> Rows { get; set; } = new();
		}

		public class SubmitChecklistRowRequest
		{
			[Required]
			public string Section { get; set; } = string.Empty;
			[Required]
			public string Label { get; set; } = string.Empty;

			// Which methods user ticked for this row (optional)
			public List<Guid> MethodIds { get; set; } = new();

			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
		}

		public class SubmitChecklistResultResponse
		{
			public bool? OverallPassed { get; set; }
			public List<Guid> InspectionIds { get; set; } = new();
		}
	}
}
