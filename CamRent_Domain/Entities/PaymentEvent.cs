using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class PaymentEvent : BaseEntity
	{
		public Guid? PaymentId { get; set; }
		public string Provider { get; set; } = "PayOS";
		public string Type { get; set; } = "ipn"; // ipn|return|manual
		public string Status { get; set; } = "received"; // received|verified|failed
		public string RequestHash { get; set; } = string.Empty; // idempotency key
		public string RawData { get; set; } = string.Empty; // serialized vnp_* or payload

		public string? ResponseCode { get; set; }
		public string? TransactionNo { get; set; }
		public string? BankCode { get; set; }
		public string? CardType { get; set; }
		public DateTime? PaidAt { get; set; }
		public decimal? Amount { get; set; }
	}
}

