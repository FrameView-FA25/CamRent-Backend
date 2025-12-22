using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.WorkSlotDTO;

namespace CamRent_Application.Services
{
	public class WorkSlotService : IWorkSlotService
	{
		private readonly IUnitOfWork _uow;

		public WorkSlotService(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public async Task<List<WorkSlotResponse>> GetSlotsAsync(CancellationToken ct = default)
		{
			var repo = _uow.Repository<WorkSlotDefinition>();
			var entities = await repo.GetAllAsync();

			return entities
				.OrderBy(ws => ws.SlotIndex)
				.Select(ws => new WorkSlotResponse
				{
					Id = ws.Id,
					SlotIndex = ws.SlotIndex,
					StartTime = ws.StartTime,
					EndTime = ws.EndTime,
					IsActive = ws.IsActive
				})
				.ToList();
		}

		public async Task<WorkSlotResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			var ws = await _uow.Repository<WorkSlotDefinition>().GetByIdAsync(id);
			if (ws == null) return null;

			return new WorkSlotResponse
			{
				Id = ws.Id,
				SlotIndex = ws.SlotIndex,
				StartTime = ws.StartTime,
				EndTime = ws.EndTime,
				IsActive = ws.IsActive
			};
		}

		public async Task<Guid> CreateAsync(CreateWorkSlotRequest request, CancellationToken ct = default)
		{
			var entity = new WorkSlotDefinition
			{
				Id = Guid.NewGuid(),
				SlotIndex = request.SlotIndex,
				StartTime = request.StartTime,
				EndTime = request.EndTime,
				IsActive = request.IsActive,
				CreatedAt = DateTime.UtcNow
			};

			await _uow.Repository<WorkSlotDefinition>().AddAsync(entity);
			await _uow.Complete();
			return entity.Id;
		}

		public async Task<bool> UpdateAsync(Guid id, UpdateWorkSlotRequest request, CancellationToken ct = default)
		{
			var repo = _uow.Repository<WorkSlotDefinition>();
			var entity = await repo.GetByIdAsync(id);
			if (entity == null) return false;

			if (request.StartTime.HasValue) entity.StartTime = request.StartTime.Value;
			if (request.EndTime.HasValue) entity.EndTime = request.EndTime.Value;
			if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

			entity.UpdatedAt = DateTime.UtcNow;
			await repo.UpdateAsync(entity);
			await _uow.Complete();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
		{
			var repo = _uow.Repository<WorkSlotDefinition>();
			var entity = await repo.GetByIdAsync(id);
			if (entity == null) return false;

			await repo.DeleteAsync(id);
			await _uow.Complete();
			return true;
		}
	}
}


