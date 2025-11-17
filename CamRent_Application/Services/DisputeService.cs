using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public sealed class DisputeService : IDisputeService
	{
		private readonly IUnitOfWork _uow;
		public DisputeService(IUnitOfWork uow) { _uow = uow; }

		public async Task<IEnumerable<DisputeDTO.DisputeResponse>> GetByBookingAsync(Guid bookingId)
		{
			var list = await _uow.Repository<Dispute>().ListAsync(d => d.BookingId == bookingId);
			return list.Select(Map);
		}

		public async Task<DisputeDTO.DisputeResponse?> GetAsync(Guid disputeId)
		{
			var d = await _uow.Repository<Dispute>().GetByIdAsync(disputeId);
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
			// Update total
			var d = await _uow.Repository<Dispute>().GetByIdAsync(disputeId) ?? throw new InvalidOperationException("Dispute not found");
			var items = await _uow.Repository<DisputeItem>().ListAsync(i => i.DisputeId == disputeId);
			d.TotalAmount = items.Sum(i => i.Amount);
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
				TotalAmount = d.TotalAmount,
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

