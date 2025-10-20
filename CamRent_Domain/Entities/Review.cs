using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Review : BaseEntity
    {
        public Guid AuthorUserId { get; set; }
        public User AuthorUser { get; set; } = default!;

        public Guid? TargetUserId { get; set; }
        public User? TargetUser { get; set; }

        public Guid? TargetDeviceId { get; set; }
        public Device? TargetDevice { get; set; }

        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

