using System;
using System.Collections.Generic;

namespace CamRent_Application.DTOs
{
	public class WalletDTO
	{
		// Dùng để tạo / log transaction (Credit / Debit)
		public class WalletTransactionRequest
		{
			public decimal Amount { get; set; }                  // Số tiền (luôn dương)
			public string Type { get; set; } = string.Empty;     // topup, pay_booking, refund, dispute...
			public Guid? PaymentId { get; set; }                 // Liên kết Payment (nếu có)
			public Guid? BookingId { get; set; }                 // Liên kết Booking (nếu có)
			public string? Description { get; set; }             // Nội dung hiển thị lịch sử ví
		}

		// DTO cho FE xem từng dòng lịch sử ví
		public class WalletTransactionResponse
		{
			public Guid Id { get; set; }
			public string Type { get; set; } = string.Empty;
			public decimal Amount { get; set; }
			public bool IsCredit { get; set; }
			public Guid? PaymentId { get; set; }
			public Guid? BookingId { get; set; }
			public string? Description { get; set; }
			public DateTime CreatedAt { get; set; }
		}

		// DTO tóm tắt ví + vài transaction gần nhất
		public class WalletSummaryResponse
		{
			public decimal Balance { get; set; }
			public decimal FrozenBalance { get; set; }
			public List<WalletTransactionResponse> RecentTransactions { get; set; } = new();
		}

		// Dùng khi tạo link nạp ví
		public class WalletTopupRequest
		{
			public decimal Amount { get; set; }
			public string ReturnUrl { get; set; } = string.Empty;
			public string CancelUrl { get; set; } = string.Empty;
		}

		// Dùng khi người dùng gửi yêu cầu rút tiền về tài khoản ngân hàng
		public class WalletWithdrawRequest
		{
			public decimal Amount { get; set; }                  // Số tiền muốn rút
			public string? Note { get; set; }                    // Ghi chú thêm nếu cần
		}

		// Dùng cho staff xem lịch sử yêu cầu rút và trạng thái xử lý
		public class WalletWithdrawHistoryItem
		{
			public Guid TransactionId { get; set; }
			public Guid UserId { get; set; }
			public decimal Amount { get; set; }
			public string Type { get; set; } = string.Empty;     // withdraw_request / withdraw / withdraw_failed
			public string? Description { get; set; }
			public DateTime CreatedAt { get; set; }

			// Thông tin ngân hàng để staff chuyển tiền
			public string? BankAccountNumber { get; set; }
			public string? BankName { get; set; }
			public string? BankAccountName { get; set; }

			// Một số thông tin nhận diện user (tiện hiển thị)
			public string? FullName { get; set; }
			public string? Email { get; set; }
		}
	}
}
