using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CamRent_Application.Services
{
	public sealed class DisputeService : IDisputeService
	{
		private readonly IUnitOfWork _uow;
		public DisputeService(IUnitOfWork uow) { _uow = uow; }

		public async Task<IEnumerable<DisputeDTO.DisputeResponse>> GetByBookingAsync(Guid bookingId)
		{
			// Lấy toàn bộ dispute của một booking, kèm theo danh sách DisputeItem (Items) để FE hiển thị chi tiết.
			var list = await _uow.Repository<Dispute>().ListAsync(
				d => d.BookingId == bookingId,
				include: q => q.Include(x => x.Items));
			return list.Select(Map);
		}

		public async Task<DisputeDTO.DisputeResponse?> GetAsync(Guid disputeId)
		{
			// Lấy 1 dispute theo Id, include luôn các DisputeItem để không bị rỗng phần Items khi trả về cho FE.
			var disputes = await _uow.Repository<Dispute>().ListAsync(
				d => d.Id == disputeId,
				include: q => q.Include(x => x.Items));
			var d = disputes.FirstOrDefault();
			if (d == null) return null;
			return Map(d);
		}

		public async Task<Guid> OpenAsync(Guid bookingId, string title, string description, string severity)
		{
			var d = new Dispute
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Title = title,
				Description = description,
				Severity = severity,
				Status = "open",
				TotalAmount = 0,
				CreatedAt = DateTime.UtcNow
			};
			await _uow.Repository<Dispute>().AddAsync(d);
			await _uow.Complete();
			return d.Id;
		}

		public async Task AddItemAsync(Guid disputeId, string type, decimal amount, string? notes)
		{
			var item = new DisputeItem
			{
				Id = Guid.NewGuid(),
				DisputeId = disputeId,
				Type = type,
				Amount = amount,
				Notes = notes,
				CreatedAt = DateTime.UtcNow
			};
			await _uow.Repository<DisputeItem>().AddAsync(item);
			var d = await _uow.Repository<Dispute>().GetByIdAsync(disputeId) ?? throw new InvalidOperationException("Khong co tranh chap nay");
			d.TotalAmount += amount;
			d.Status = "under_review"; // Auto-move status when new item added
			await _uow.Repository<Dispute>().UpdateAsync(d);
			await _uow.Complete();
		}

		public async Task DeleteItemAsync(Guid disputeId, Guid itemId)
		{
			var itemRepo = _uow.Repository<DisputeItem>();
			var disputeRepo = _uow.Repository<Dispute>();

			var item = await itemRepo.GetByIdAsync(itemId);
			if (item == null || item.DisputeId != disputeId)
				throw new InvalidOperationException("Khong co dispute item nay");

			var d = await disputeRepo.GetByIdAsync(disputeId) ?? throw new InvalidOperationException("Khong co tranh chap nay");
			var items = await itemRepo.ListAsync(i => i.DisputeId == disputeId);
			d.TotalAmount = items.Where(i => i.Id != itemId).Sum(i => i.Amount);

			await itemRepo.DeleteAsync(itemId);
			await disputeRepo.UpdateAsync(d);
			await _uow.Complete();
		}

		public Task AssignAsync(Guid disputeId, Guid? userId)
		{
			// For now just a placeholder (requires extra field); no-op
			return Task.CompletedTask;
		}

		public async Task UpdateStatusAsync(Guid disputeId, string status)
		{
			var d = await _uow.Repository<Dispute>().GetByIdAsync(disputeId) ?? throw new InvalidOperationException("Không có tranh chấp này");
			d.Status = status;
			d.UpdatedAt = DateTime.UtcNow;
			await _uow.Repository<Dispute>().UpdateAsync(d);
			await _uow.Complete();
		}

		private static DisputeDTO.DisputeResponse Map(Dispute d)
		{
			return new DisputeDTO.DisputeResponse
			{
				Id = d.Id,
				BookingId = d.BookingId,
				Title = d.Title,
				Description = d.Description,
				Severity = d.Severity,
				Status = d.Status,
				CreatedAt = d.CreatedAt,
				UpdatedAt = d.UpdatedAt,
				// Nếu vì lý do nào đó TotalAmount chưa được cập nhật, fallback tính từ Items.
				TotalAmount = d.TotalAmount != 0 ? d.TotalAmount : d.Items.Sum(i => i.Amount),
				Items = d.Items.Select(i => new DisputeDTO.DisputeItemResponse
				{
					Id = i.Id,
					Type = i.Type,
					Amount = i.Amount,
					Notes = i.Notes,
					CreatedAt = i.CreatedAt
				}).ToList()
			};
		}

		public async Task<decimal> CalculateTotalDisputeAmountByBookingIdAsync(Guid bookingId)
		{
			var disputes = await _uow.Repository<Dispute>().ListAsync(d => d.BookingId == bookingId);
			var total = disputes.Sum(d => d.TotalAmount);
			return total;
		}
	}
}

