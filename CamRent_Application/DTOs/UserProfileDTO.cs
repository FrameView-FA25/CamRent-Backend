using CamRent_Domain.Common;

namespace CamRent_Application.DTOs
{
	public class UserProfileDTO
	{
		public class UserProfileResponse
		{
			public Guid Id { get; set; }
			public string Email { get; set; } = string.Empty;
			public string Phone { get; set; } = string.Empty;
			public string FullName { get; set; } = string.Empty;
			public Address? Address { get; set; }
			public UserStatus Status { get; set; }
			public string? BankAccountNumber { get; set; }
			public string? BankName { get; set; }
			public string? BankAccountName { get; set; }
			public Guid? SignatureAssetId { get; set; }
			public Guid? AvatarId { get; set; }
			public string? AvatarUrl { get; set; }
			public string? SignatureUrl { get; set; }
			public List<string> Roles { get; set; } = new();

			// Thông tin chi nhánh (nếu user là Staff hoặc BranchManager)
			public BranchInfo? Branch { get; set; }
		}

		public class BranchInfo
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = string.Empty;
			public Address Address { get; set; } = new Address();
			public bool IsManager { get; set; } // true nếu là BranchManager, false nếu là Staff
		}
	}
}

