using CamRent_Domain.Common;
using CamRent_Domain.Users;

namespace CamRent_Domain.Branches
{
    public class Branch : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public Address Address { get; set; } = new Address();
    }

    public class UserBranchMembership : BaseEntity
    {
        public Guid UserId { get; set; }
        public Users.User User { get; set; } = default!;

        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = default!;
    }
}

