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
		public class InspectionRequest
		{
			public Guid? ItemId { get; set; }
			public ItemType ItemType { get; set; }
			public InspectionType Type { get; set; }
			public Guid? InspectionTypeId { get; set; } // BookingId or VerifyRequestId
			public string Section { get; set; } = string.Empty;
			public string Label { get; set; } = string.Empty;
			public string? Value { get; set; }
			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
		}
		public class InspectionResponseDTO
		{
			public Guid Id { get; set; }
			public string? ItemName { get; set; }
			public ItemType ItemType { get; set; }
			public string Section { get; set; } = string.Empty;
			public string Label { get; set; } = string.Empty;
			public string? Value { get; set; }
			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
			public List<FileAssetDTO> Media { get; set; } = new();
		}
	}
}
