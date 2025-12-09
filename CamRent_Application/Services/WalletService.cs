using AutoMapper;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.WalletDTO;

namespace CamRent_Application.Services
{
	public class WalletService : IWalletService
	{
		private readonly IUnitOfWork _uow;
		private readonly IMapper _mapper;

		public WalletService(IUnitOfWork uow, IMapper mapper)
		{
			_uow = uow;
			_mapper = mapper;
		}

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

		public async Task<decimal> GetBalanceAsync(Guid userId)
		{
			var wallet = await GetOrCreateAsync(userId);
			return wallet.Balance;
		}

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
	}
}
