using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class BookingItem : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public Guid? CameraId { get; set; }
        public Camera? Camera { get; set; }

        public Guid? AccessoryId { get; set; }
        public Accessory? Accessory { get; set; }

        public Guid? ComboId { get; set; }
        public Combo? Combo { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DepositAmount { get; set; }

    }
}

