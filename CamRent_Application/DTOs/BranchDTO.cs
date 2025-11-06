using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class BranchDTO
	{
		public class BranchRequest
		{
			public string Name { get; set; } = string.Empty;
			public Address Address { get; set; } = new Address();
			public Guid? ManagerId { get; set; }
		}

		public class BranchResponse
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = string.Empty;
			public Address Address { get; set; } = new Address();
			public Guid ManagerId { get; set; }
			public string ManagerName { get; set; } = string.Empty;

			public ICollection<UserBranchMembership> UserMemberships { get; set; } = new List<UserBranchMembership>();
		}

		public class BranchMembership
		{
			public Guid UserId { get; set; }
			public string FullName { get; set; } = string.Empty;
			public string Phone { get; set; } = string.Empty;
			public string Email { get; set; } = string.Empty;
		}
	}
}
