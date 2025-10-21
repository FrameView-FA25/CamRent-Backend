using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public string? NationalIdNumber { get; set; }
        public string KycStatus { get; set; } = "pending"; // pending, approved, rejected

        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountName { get; set; }
    }
}

