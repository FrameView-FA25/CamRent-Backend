using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.InspectionChecklistDTO;

namespace CamRent_Application.Services
{
	public class InspectionMethodService : IInspectionMethodService
	{
		private readonly IUnitOfWork _unitOfWork;

		public InspectionMethodService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<List<InspectionMethodResponse>> ListAsync(bool includeInactive = false)
		{
			var methods = (await _unitOfWork.Repository<InspectionMethod>()
				.ListAsync(
					filter: m => includeInactive || m.IsActive,
					orderBy: q => q.OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
				)).ToList();

			return methods.Select(Map).ToList();
		}

		public async Task<InspectionMethodResponse?> GetByIdAsync(Guid id)
		{
			var method = await _unitOfWork.Repository<InspectionMethod>().GetByIdAsync(id);
			return method == null ? null : Map(method);
		}

		public async Task<Guid> CreateAsync(UpsertInspectionMethodRequest request, Guid adminId)
		{
			var code = (request.Code ?? string.Empty).Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Code is required.");

			var exists = await _unitOfWork.Repository<InspectionMethod>().AnyAsync(m => m.Code == code);
			if (exists) throw new InvalidOperationException("Method code already exists.");

			var entity = new InspectionMethod
			{
				Code = code,
				Name = (request.Name ?? string.Empty).Trim(),
				SortOrder = request.SortOrder,
				IsActive = request.IsActive,
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = adminId
			};

			await _unitOfWork.Repository<InspectionMethod>().AddAsync(entity);
			await _unitOfWork.Complete();
			return entity.Id;
		}

		public async Task<int> UpdateAsync(Guid id, UpsertInspectionMethodRequest request, Guid adminId)
		{
			var entity = await _unitOfWork.Repository<InspectionMethod>().GetByIdAsync(id);
			if (entity == null) return 0;

			var code = (request.Code ?? string.Empty).Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Code is required.");

			var exists = await _unitOfWork.Repository<InspectionMethod>().AnyAsync(m => m.Id != id && m.Code == code);
			if (exists) throw new InvalidOperationException("Method code already exists.");

			entity.Code = code;
			entity.Name = (request.Name ?? string.Empty).Trim();
			entity.SortOrder = request.SortOrder;
			entity.IsActive = request.IsActive;
			entity.UpdatedAt = DateTime.UtcNow;
			entity.UpdatedByUserId = adminId;

			await _unitOfWork.Repository<InspectionMethod>().UpdateAsync(entity);
			return await _unitOfWork.Complete();
		}

		public async Task<int> DeleteAsync(Guid id)
		{
			// block delete if referenced
			var usedInTemplate = await _unitOfWork.Repository<InspectionChecklistItemAllowedMethod>()
				.AnyAsync(x => x.MethodId == id);
			if (usedInTemplate) throw new InvalidOperationException("Method is used by a checklist template.");

			var usedInResult = await _unitOfWork.Repository<InspectionMethodSelection>()
				.AnyAsync(x => x.MethodId == id);
			if (usedInResult) throw new InvalidOperationException("Method is used by inspection results.");

			await _unitOfWork.Repository<InspectionMethod>().DeleteAsync(id);
			return await _unitOfWork.Complete();
		}

		private static InspectionMethodResponse Map(InspectionMethod m) => new()
		{
			Id = m.Id,
			Code = m.Code,
			Name = m.Name,
			SortOrder = m.SortOrder,
			IsActive = m.IsActive
		};
	}
}
