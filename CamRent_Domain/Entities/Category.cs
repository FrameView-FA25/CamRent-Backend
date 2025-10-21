using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public Category? Parent { get; set; }
    }

    public class DeviceCategoryLink : BaseEntity
    {
        public Guid DeviceId { get; set; }
        public Device Device { get; set; } = default!;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = default!;
    }
}

