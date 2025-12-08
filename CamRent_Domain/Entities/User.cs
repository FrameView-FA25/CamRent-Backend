using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace CamRent_Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public Address? Address { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;

        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountName { get; set; }
		public Guid? SignatureAssetId { get; set; }
		public FileAsset? SignatureAsset { get; set; }
        public Guid? AvatarId { get; set; }
        public FileAsset? Avatar { get; set; }

		public ICollection<UserRoleMapping> Roles { get; set; } = new List<UserRoleMapping>();
	}

    public class ResetPasswordToken : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
        public string Token { get; set; } = string.Empty; // random, single-use
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public bool IsUsed { get; set; }
    }

    public class UserRoleMapping : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public UserRole Role { get; set; }
    }
}

