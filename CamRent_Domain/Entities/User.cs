using CamRent_Domain.Common;

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

        // Merged profile fields
        public string? NationalIdNumber { get; set; }
        public string KycStatus { get; set; } = "pending";
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountName { get; set; }

        public ICollection<UserRoleMapping> Roles { get; set; } = new List<UserRoleMapping>();
        public ICollection<UserBranchMembership> BranchMemberships { get; set; } = new List<UserBranchMembership>();
    }

    public class UserRoleMapping : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public UserRole Role { get; set; }
    }
}

