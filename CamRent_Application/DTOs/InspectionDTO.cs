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
			public string Section { get; set; } = string.Empty;
			public string Label { get; set; } = string.Empty;
			public string? Value { get; set; }
			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
			public string? ChecklistTemplateVersion { get; set; }
			public List<IFormFile>? Media { get; set; }
		}
		public class InspectionResponseDTO
		{
			public Guid Id { get; set; }
			public string Section { get; set; } = string.Empty;
			public string Label { get; set; } = string.Empty;
			public string? Value { get; set; }
			public bool? Passed { get; set; }
			public string Notes { get; set; } = string.Empty;
			public string? ChecklistTemplateVersion { get; set; }
			public List<FileAssetDTO> Media { get; set; } = new();
		}
	}
}
