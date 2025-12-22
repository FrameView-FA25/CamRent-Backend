using CamRent_Domain.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class InspectionDTO
	{
		// When using "phiếu" (InspectionForm), row updates should go through inspection-forms API.
		// This request is kept for uploading/removing media for a specific inspection row.
		public class UpdateInspectionMediaRequest
		{
			public List<IFormFile>? Files { get; set; }
			public List<Guid>? RemoveMediaIds { get; set; }
		}
		public class InspectionResponseDTO
		{
			public Guid Id { get; set; }
			public Guid? FormId { get; set; }
			public string? ItemName { get; set; }
			public ItemType ItemType { get; set; }
			public string Section { get; set; } = string.Empty;
			public string Label { get; set; } = string.Empty;
			public string? Value { get; set; }
			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
			public DateTime CreatedAt { get; set; }
			public List<FileAssetDTO> Media { get; set; } = new();
			public List<InspectionChecklistDTO.InspectionMethodResponse> Methods { get; set; } = new();
		}
	}
}
