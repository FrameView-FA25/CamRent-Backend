using CamRent_Domain.Entities;
using System;
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
	}
}