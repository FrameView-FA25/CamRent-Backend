using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Branch : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public Address Address { get; set; } = new Address();

        public Guid? ManagerId { get; set; }
		public User? Manager { get; set; }

		public ICollection<UserBranchMembership> UserMemberships { get; set; } = new List<UserBranchMembership>();
	}

    public class UserBranchMembership : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid BranchId { get; set; }
        public Branch Branch { get; set; }
    }
}

