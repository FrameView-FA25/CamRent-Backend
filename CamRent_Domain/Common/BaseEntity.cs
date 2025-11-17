using System;

namespace CamRent_Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }

        // store as DateTimeOffset to include timezone offset (we will use +07:00 for VN)
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
    }
}

