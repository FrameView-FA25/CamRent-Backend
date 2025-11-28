using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class ContractDTO
	{
		public class ContractResponse
		{
			public Guid Id { get; set; }        // Booking / Verification
			public ContractStatus Status { get; set; }
			public string? BranchName { get; set; }
			public string? BranchAddress { get; set; }
			public DateTime CreatedAt { get; set; }
			public DateTime? SignedAt { get; set; }
		}
	}
}
