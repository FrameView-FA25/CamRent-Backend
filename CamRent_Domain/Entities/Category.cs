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
        public Guid? CameraId { get; set; }
        public Camera? Camera { get; set; }

        public Guid? AccessoryId { get; set; }
        public Accessory? Accessory { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = default!;
    }
}

