using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public PaymentStatus Status { get; set; }
        public string Provider { get; set; } = "PayOS";
		/// <summary>
		/// booking | wallet_topup | wallet_withdraw | dispute | ...
		/// </summary>
		public string Purpose { get; set; } = "booking";
		public string? ProviderPaymentId { get; set; }
        public decimal AuthorizedAmount { get; set; }
        public decimal CapturedAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public ICollection<PaymentLine> Lines { get; set; } = new List<PaymentLine>();
    }
}