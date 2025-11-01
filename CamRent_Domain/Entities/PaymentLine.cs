using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class PaymentLine : BaseEntity
    {
        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; } = default!;

        public string Type { get; set; } = string.Empty; // rental, deposit, delivery_fee, adjustment
        public decimal Amount { get; set; }
        public decimal CapturedAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public string Currency { get; set; } = "VND";
    }
}

