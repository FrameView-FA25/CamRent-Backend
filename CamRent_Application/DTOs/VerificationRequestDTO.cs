using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			public string Status { get; set; } = "pending"; // pending, approved, rejected
			public Guid? StaffId { get; set; }
			public string StaffName { get; set; } 
			public Guid? BranchId { get; set; }
			public string? BranchName { get; set; }
			public string? Address { get; set; }

			public string? Notes { get; set; }

			public Guid? CreatedByUserId { get; set; }
		}

		public class CreateVerificationRequestDTO
		{
			public string? Name { get; set; }
			public string PhoneNumber { get; set; } = string.Empty;
			public DateTime InspectionDate { get; set; }
			public string? Notes { get; set; }
			public Guid? BranchId { get; set; }

		}
	}
}
