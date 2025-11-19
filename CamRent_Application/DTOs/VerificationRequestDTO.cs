using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.InspectionDTO;

namespace CamRent_Application.DTOs
{
	public class VerificationRequestDTO
	{
		public class VerificationResponseDTO
		{
			public Guid Id { get; set; }
			public string? Name { get; set; }
			public string? PhoneNumber { get; set; }
			public DateTime InspectionDate { get; set; }
			public VerificationStatus Status { get; set; } 
			public Guid? StaffId { get; set; }
			public string? StaffName { get; set; }   // nên cho nullable cho an toàn

			public Guid? BranchId { get; set; }
			public string? BranchName { get; set; }
			public string? Address { get; set; }

			public string? Notes { get; set; }

			public Guid? CreatedByUserId { get; set; }

			public List<VerificationItemDTO>? Items { get; set; } 
			public List<InspectionResponseDTO>? Inspections { get; set; } 
		}


		public class CreateVerificationRequestDTO
		{
			public string? Name { get; set; }
			public string PhoneNumber { get; set; } = string.Empty;
			public DateTime InspectionDate { get; set; }
			public Guid? BranchId { get; set; }
			public List<VerificationItemDTO>? Items { get; set; }
		}

		// New: update DTO - partial updates supported (nullable fields)
		public class UpdateVerificationRequestDTO
		{
			public string? Name { get; set; }
			public string? PhoneNumber { get; set; }
			public DateTime? InspectionDate { get; set; }
			public Guid? BranchId { get; set; }
			public List<VerificationItemDTO>? Items { get; set; }
		}

		public class VerificationItemDTO
		{
			public Guid? ItemId { get; set; }
			public string? ItemName { get; set; }
			public ItemType ItemType { get; set; }

		}
	}
}
