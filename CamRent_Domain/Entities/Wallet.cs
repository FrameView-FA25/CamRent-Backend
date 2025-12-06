using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Domain.Entities
{
	public class Wallet : BaseEntity
	{
		public Guid UserId { get; set; }
		public User User { get; set; } = default!;

		public decimal Balance { get; set; }          // Số tiền khả dụng
		public decimal FrozenBalance { get; set; }    // Nếu sau này muốn “tạm giữ”
		public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
	}

	public class WalletTransaction : BaseEntity
	{
		public Guid WalletId { get; set; }
		public Wallet Wallet { get; set; } = default!;

		public string Type { get; set; } = string.Empty;
		// topup, pay_booking, refund, dispute_charge, dispute_refund ...
		public string Description { get; set; } = string.Empty;

		public decimal Amount { get; set; }         // Luôn là số dương
		public bool IsCredit { get; set; }          // true = cộng vào ví, false = trừ ví

		public Guid? PaymentId { get; set; }
		public Payment? Payment { get; set; }        // Liên kết với Payment (nếu có)
		public Guid? BookingId { get; set; }
		public Booking? Booking { get; set; } // Liên kết booking (nếu có)
	}
}
