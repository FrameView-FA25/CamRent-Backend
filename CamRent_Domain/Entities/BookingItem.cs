using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class BookingItem : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public Guid? DeviceId { get; set; }
        public Device? Device { get; set; }

        public Guid? ComboId { get; set; }
        public Combo? Combo { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DepositAmount { get; set; }
    }
}

