using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Combo : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? PriceOverride { get; set; }
        public decimal? DepositOverride { get; set; }

		public ICollection<ComboItem> Items { get; set; } = new List<ComboItem>();
    }

    public class ComboItem : BaseEntity
    {
        public Guid ComboId { get; set; }
        public Combo Combo { get; set; } = default!;

        public Guid? CameraId { get; set; }
        public Camera? Camera { get; set; }

        public Guid? AccessoryId { get; set; }
        public Accessory? Accessory { get; set; }
        public int Quantity { get; set; } = 1;
    }
}

