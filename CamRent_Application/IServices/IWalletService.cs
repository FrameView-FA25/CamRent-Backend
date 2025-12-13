using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.WalletDTO;

namespace CamRent_Application.IServices
{
	public interface IWalletService
	{
		// Lấy hoặc tạo ví mới cho user
		Task<Wallet> GetOrCreateAsync(Guid userId);

		// Chỉ lấy số dư
		Task<decimal> GetBalanceAsync(Guid userId);

		// Lấy tóm tắt ví + một số transaction gần nhất
		Task<WalletSummaryResponse> GetSummaryAsync(Guid userId, int take = 20);

		// Cộng tiền
		Task CreditAsync(Guid userId, WalletTransactionRequest req);

		// Trừ tiền
		Task<bool> DebitAsync(Guid userId, WalletTransactionRequest req);

		// Người dùng gửi yêu cầu rút tiền về ngân hàng (chỉ log transaction, không xử lý payout)
		Task<bool> RequestWithdrawAsync(Guid userId, decimal amount, string? note = null);

		// Staff xem lịch sử các yêu cầu rút (đơn giản lọc theo type WalletTransaction)
		Task<IReadOnlyList<WalletWithdrawHistoryItem>> GetWithdrawHistoryAsync(int take = 50);

		// Staff đánh dấu yêu cầu rút đã xử lý thành công (đã chuyển tiền cho user)
		Task<bool> CompleteWithdrawAsync(Guid withdrawRequestTransactionId, string? note = null);

		// Staff đánh dấu yêu cầu rút thất bại/hủy và hoàn tiền lại vào ví nếu đã trừ
		Task<bool> FailWithdrawAsync(Guid withdrawRequestTransactionId, string? note = null);
	}
}