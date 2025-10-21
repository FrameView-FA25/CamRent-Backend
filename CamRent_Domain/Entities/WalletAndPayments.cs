using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class Wallet : BaseEntity
    {
        public Guid OwnerUserId { get; set; }
        public User OwnerUser { get; set; } = default!;
        public decimal Balance { get; set; }
    }

    public class Transaction : BaseEntity
    {
        public Guid WalletId { get; set; }
        public Wallet Wallet { get; set; } = default!;

        public string Type { get; set; } = string.Empty; // debit/credit categories
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string? Reference { get; set; }
    }

    public class Payment : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public PaymentStatus Status { get; set; }
        public string Provider { get; set; } = "VNPay";
        public string? ProviderPaymentId { get; set; }
        public decimal AuthorizedAmount { get; set; }
        public decimal CapturedAmount { get; set; }
        public decimal RefundedAmount { get; set; }
    }
}

