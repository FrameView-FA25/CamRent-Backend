using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.ContractDTO;
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
			public string? StaffName { get; set; }

			public Guid? BranchId { get; set; }
			public string? BranchName { get; set; }
			public string? Address { get; set; }

			public string? Notes { get; set; }

			public DateTime CreatedAt { get; set; }
			public Guid? CreatedByUserId { get; set; }

			public List<VerificationItemDTO>? Items { get; set; }
			public ICollection<ContractResponse>? Contracts { get; set; }
			public List<InspectionResponseDTO>? Inspections { get; set; }
		}

		public class CreateVerificationRequestDTO
		{
			public string? Name { get; set; }
			[Required]
			public string PhoneNumber { get; set; } = string.Empty;
			[Required]
			public DateTime InspectionDate { get; set; }
			public Guid? BranchId { get; set; }
			public List<VerificationItemDTO>? Items { get; set; }
		}

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
			public string? SerialNumber { get; set; }
			public decimal UnitPrice { get; set; }
			public decimal DepositAmount { get; set; }
		}
	}
}
