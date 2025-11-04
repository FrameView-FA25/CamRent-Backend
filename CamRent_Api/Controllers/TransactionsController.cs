using CamRent_Application.Interfaces;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class TransactionsController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		public TransactionsController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		// Lịch sử giao dịch theo người dùng (wallet owner)
		[HttpGet("by-user/{userId:guid}")]
		public async Task<ActionResult<IEnumerable<Transaction>>> GetByUser(Guid userId)
		{
			var wallets = await _unitOfWork.Repository<Wallet>().ListAsync(w => w.OwnerUserId == userId);
			var walletIds = wallets.Select(w => w.Id).ToHashSet();
			var txs = await _unitOfWork.Repository<Transaction>().ListAsync(
				filter: t => walletIds.Contains(t.WalletId),
				orderBy: q => q.OrderByDescending(t => t.CreatedAt)
			);
			return Ok(txs);
		}

		// Lịch sử giao dịch theo booking
		[HttpGet("by-booking/{bookingId:guid}")]
		public async Task<ActionResult<IEnumerable<Transaction>>> GetByBooking(Guid bookingId)
		{
			var txs = await _unitOfWork.Repository<Transaction>().ListAsync(
				filter: t => t.BookingId == bookingId,
				orderBy: q => q.OrderByDescending(t => t.CreatedAt)
			);
			return Ok(txs);
		}
	}
}


