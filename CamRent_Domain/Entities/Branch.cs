using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Branch : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public Address Address { get; set; } = new Address();
    }

    public class UserBranchMembership : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = default!;
    }
}

