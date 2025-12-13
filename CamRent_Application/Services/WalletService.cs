using AutoMapper;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using CamRent_Domain.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.WalletDTO;

namespace CamRent_Application.Services
{
	/// <summary>
	/// Xử lý nghiệp vụ ví điện tử nội bộ cho người dùng:
	/// - Khởi tạo/lấy ví
	/// - Ghi nhận biến động số dư (credit/debit)
	/// - Quản lý quy trình rút tiền (yêu cầu rút, hoàn tất, thất bại).
	/// </summary>
	public class WalletService : IWalletService
	{
		private readonly IUnitOfWork _uow;
		private readonly IMapper _mapper;

		public WalletService(IUnitOfWork uow, IMapper mapper)
		{
			_uow = uow;
			_mapper = mapper;
		}

		/// <summary>
		/// Lấy ví hiện có của user; nếu chưa tồn tại thì khởi tạo ví mới với số dư = 0.
		/// </summary>
		public async Task<Wallet> GetOrCreateAsync(Guid userId)
		{
			var repo = _uow.Repository<Wallet>();
			var wallet = (await repo.ListAsync(w => w.UserId == userId)).FirstOrDefault();
			if (wallet != null) return wallet;

			wallet = new Wallet
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				Balance = 0,
				FrozenBalance = 0,
				CreatedAt = DateTime.UtcNow
			};

			await repo.AddAsync(wallet);
			await _uow.Complete();
			return wallet;
		}

		/// <summary>
		/// Lấy số dư hiện tại của ví (Balance) cho một user.
		/// </summary>
		public async Task<decimal> GetBalanceAsync(Guid userId)
		{
			var wallet = await GetOrCreateAsync(userId);
			return wallet.Balance;
		}

		/// <summary>
		/// Lấy thông tin tổng quan của ví và một số transaction gần nhất cho user.
		/// </summary>
		public async Task<WalletSummaryResponse> GetSummaryAsync(Guid userId, int take = 20)
		{
			var wallet = await GetOrCreateAsync(userId);

			// Lấy transaction của ví, sort mới nhất trước
			var txRepo = _uow.Repository<WalletTransaction>();
			var allTx = await txRepo.ListAsync(t => t.WalletId == wallet.Id);
			var orderedTx = allTx
				.OrderByDescending(t => t.CreatedAt)
				.Take(take)
				.ToList();

			var summary = _mapper.Map<WalletSummaryResponse>(wallet);
			summary.RecentTransactions = _mapper.Map<List<WalletTransactionResponse>>(orderedTx);

			return summary;
		}

		/// <summary>
		/// Cộng tiền vào ví (credit), ví dụ sau khi user nạp tiền hoặc refund từ hệ thống.
		/// Đồng thời log một bản ghi WalletTransaction tương ứng.
		/// </summary>
		public async Task CreditAsync(Guid userId, WalletTransactionRequest req)
		{
			if (req.Amount <= 0)
				throw new ArgumentException("Amount must be greater than 0 for credit.", nameof(req.Amount));

			var wallet = await GetOrCreateAsync(userId);

			wallet.Balance += req.Amount;
			wallet.UpdatedAt = DateTime.UtcNow;

			await _uow.Repository<Wallet>().UpdateAsync(wallet);

			var tx = new WalletTransaction
			{
				Id = Guid.NewGuid(),
				WalletId = wallet.Id,
				Type = req.Type,         // "topup"
				Amount = req.Amount,
				IsCredit = true,
				PaymentId = req.PaymentId,
				BookingId = req.BookingId,
				Description = req.Description ?? string.Empty,
				CreatedAt = DateTime.UtcNow
			};

			await _uow.Repository<WalletTransaction>().AddAsync(tx);
			await _uow.Complete();
		}

		/// <summary>
		/// Trừ tiền khỏi ví (debit), ví dụ khi thanh toán booking.
		/// Nếu số dư không đủ thì trả về false và không tạo transaction.
		/// </summary>
		public async Task<bool> DebitAsync(Guid userId, WalletTransactionRequest req)
		{
			var wallet = await GetOrCreateAsync(userId);

			if (wallet.Balance < req.Amount)
				return false;

			wallet.Balance -= req.Amount;
			wallet.UpdatedAt = DateTime.UtcNow;

			await _uow.Repository<Wallet>().UpdateAsync(wallet);

			var tx = new WalletTransaction
			{
				Id = Guid.NewGuid(),
				WalletId = wallet.Id,
				Type = req.Type,
				Amount = req.Amount,
				IsCredit = false,
				PaymentId = req.PaymentId,
				BookingId = req.BookingId,
				Description = req.Description ?? string.Empty,
				CreatedAt = DateTime.UtcNow
			};

			await _uow.Repository<WalletTransaction>().AddAsync(tx);
			await _uow.Complete();

			return true;
		}

		/// <summary>
		/// Người dùng gửi yêu cầu rút tiền:
		/// - Kiểm tra số dư
		/// - Trừ số tiền đó khỏi Balance ngay lập tức (tiền được \"giữ\" để chờ staff xử lý)
		/// - Log một dòng WalletTransaction với Type = \"withdraw_request\".
		/// Việc chuyển tiền thật sẽ do staff xử lý offline bằng QR; nếu thất bại sẽ dùng FailWithdrawAsync để hoàn tiền.
		/// </summary>
		public async Task<bool> RequestWithdrawAsync(Guid userId, decimal amount, string? note = null)
		{
			if (amount <= 0) throw new ArgumentException("Amount must be greater than 0.", nameof(amount));

			var wallet = await GetOrCreateAsync(userId);

			// Nếu đã có tiền đang bị giữ (đang có yêu cầu rút before), không cho tạo thêm.
			if (wallet.FrozenBalance > 0)
				return false;

			if (wallet.Balance < amount)
				return false;

			// Dời tiền từ Balance sang FrozenBalance để \"giữ\" lại, chờ staff xử lý.
			wallet.Balance -= amount;
			wallet.FrozenBalance += amount;
			wallet.UpdatedAt = DateTime.UtcNow;
			await _uow.Repository<Wallet>().UpdateAsync(wallet);

			var tx = new WalletTransaction
			{
				Id = Guid.NewGuid(),
				WalletId = wallet.Id,
				Type = "withdraw_request",
				Amount = amount,
				IsCredit = false,
				Description = note ?? "Yêu cầu rút tiền về tài khoản ngân hàng",
				CreatedAt = DateTime.UtcNow
			};

			await _uow.Repository<WalletTransaction>().AddAsync(tx);
			await _uow.Complete();

			return true;
		}

		/// <summary>
		/// Lấy một số dòng lịch sử rút tiền (withdraw_request/withdraw/withdraw_failed) mới nhất.
		/// Dùng cho màn quản trị của Staff để xem và xử lý.
		/// </summary>
		public async Task<IReadOnlyList<WalletWithdrawHistoryItem>> GetWithdrawHistoryAsync(int take = 50)
		{
			take = Math.Clamp(take, 1, 200);

			var txRepo = _uow.Repository<WalletTransaction>();
			var allTx = await txRepo.ListAsync(t =>
				t.Type == "withdraw_request" ||
				t.Type == "withdraw" ||
				t.Type == "withdraw_failed");

			var ordered = allTx
				.OrderByDescending(t => t.CreatedAt)
				.Take(take)
				.ToList();

			// Cần map WalletId -> UserId và lấy thông tin ngân hàng tương ứng
			var walletRepo = _uow.Repository<Wallet>();
			var walletIds = ordered.Select(t => t.WalletId).Distinct().ToList();
			var wallets = await walletRepo.ListAsync(w => walletIds.Contains(w.Id));
			var walletLookup = wallets.ToDictionary(w => w.Id, w => w.UserId);

			// Load thêm thông tin User để có STK/ngân hàng
			var userRepo = _uow.Repository<User>();
			var userIds = wallets.Select(w => w.UserId).Distinct().ToList();
			var users = await userRepo.ListAsync(u => userIds.Contains(u.Id));
			var userLookup = users.ToDictionary(u => u.Id, u => u);

			var result = ordered.Select(t =>
			{
				walletLookup.TryGetValue(t.WalletId, out var uid);
				userLookup.TryGetValue(uid, out var user);

				return new WalletWithdrawHistoryItem
				{
					TransactionId = t.Id,
					UserId = uid,
					Amount = t.Amount,
					Type = t.Type,
					Description = t.Description,
					CreatedAt = t.CreatedAt,
					BankAccountNumber = user?.BankAccountNumber,
					BankName = user?.BankName,
					BankAccountName = user?.BankAccountName,
					FullName = user?.FullName,
					Email = user?.Email
				};
			}).ToList();

			return result;
		}

		/// <summary>
		/// Staff đánh dấu yêu cầu rút đã được chuyển tiền thành công.
		/// Tiền đã bị trừ khỏi ví ở bước RequestWithdrawAsync, nên ở đây chỉ log thêm 1 transaction \"withdraw\" để lịch sử rõ ràng.
		/// </summary>
		public async Task<bool> CompleteWithdrawAsync(Guid withdrawRequestTransactionId, string? note = null)
		{
			var txRepo = _uow.Repository<WalletTransaction>();
			var walletRepo = _uow.Repository<Wallet>();

			var reqTx = await txRepo.GetByIdAsync(withdrawRequestTransactionId);
			if (reqTx == null || !string.Equals(reqTx.Type, "withdraw_request", StringComparison.OrdinalIgnoreCase))
				return false;

			// Giảm số tiền đang giữ (FrozenBalance) khi đã chuyển xong.
			var wallet = await walletRepo.GetByIdAsync(reqTx.WalletId);
			if (wallet == null)
				return false;

			if (wallet.FrozenBalance >= reqTx.Amount)
			{
				wallet.FrozenBalance -= reqTx.Amount;
				wallet.UpdatedAt = DateTime.UtcNow;
				await walletRepo.UpdateAsync(wallet);
			}

			var completedTx = new WalletTransaction
			{
				Id = Guid.NewGuid(),
				WalletId = reqTx.WalletId,
				Type = "withdraw",
				Amount = reqTx.Amount,
				IsCredit = false,
				Description = note ?? $"Hoàn tất rút tiền cho yêu cầu {reqTx.Id}",
				CreatedAt = DateTime.UtcNow
			};

			await txRepo.AddAsync(completedTx);
			await _uow.Complete();

			return true;
		}

		/// <summary>
		/// Staff đánh dấu yêu cầu rút thất bại/hủy:
		/// - Cộng lại số tiền đã trừ vào Balance
		/// - Log một transaction \"withdraw_failed\" với IsCredit = true.
		/// </summary>
		public async Task<bool> FailWithdrawAsync(Guid withdrawRequestTransactionId, string? note = null)
		{
			var txRepo = _uow.Repository<WalletTransaction>();
			var walletRepo = _uow.Repository<Wallet>();

			var reqTx = await txRepo.GetByIdAsync(withdrawRequestTransactionId);
			if (reqTx == null || !string.Equals(reqTx.Type, "withdraw_request", StringComparison.OrdinalIgnoreCase))
				return false;

			var wallet = await walletRepo.GetByIdAsync(reqTx.WalletId);
			if (wallet == null)
				return false;

			// Hoàn tiền lại vào ví: dời tiền từ FrozenBalance về Balance
			if (wallet.FrozenBalance >= reqTx.Amount)
			{
				wallet.FrozenBalance -= reqTx.Amount;
				wallet.Balance += reqTx.Amount;
				wallet.UpdatedAt = DateTime.UtcNow;
				await walletRepo.UpdateAsync(wallet);
			}

			var failTx = new WalletTransaction
			{
				Id = Guid.NewGuid(),
				WalletId = reqTx.WalletId,
				Type = "withdraw_failed",
				Amount = reqTx.Amount,
				IsCredit = true,
				Description = note ?? $"Hủy yêu cầu rút tiền {reqTx.Id}, hoàn lại tiền vào ví",
				CreatedAt = DateTime.UtcNow
			};

			await txRepo.AddAsync(failTx);
			await _uow.Complete();

			return true;
		}
	}
}
