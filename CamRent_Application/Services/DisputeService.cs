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
			// Sau khi thêm item, cập nhật lại TotalAmount = tổng Amount của tất cả DisputeItem.
			var d = await _uow.Repository<Dispute>().GetByIdAsync(disputeId) ?? throw new InvalidOperationException("Dispute not found");
			var items = await _uow.Repository<DisputeItem>().ListAsync(i => i.DisputeId == disputeId);
			d.TotalAmount = items.Sum(i => i.Amount);
			d.Status = "under_review"; // Tự động chuyển trạng thái khi có item mới
			await _uow.Repository<Dispute>().UpdateAsync(d);
			await _uow.Complete();
		}

		public Task AssignAsync(Guid disputeId, Guid? userId)
		{
			// For now just a placeholder (requires extra field); no-op
			return Task.CompletedTask;
		}

		public async Task UpdateStatusAsync(Guid disputeId, string status, string? resolutionNote)
		{
			var d = await _uow.Repository<Dispute>().GetByIdAsync(disputeId) ?? throw new InvalidOperationException("Dispute not found");
			d.Status = status;
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
					Notes = i.Notes
				}).ToList()
			};
		}
	}
}

