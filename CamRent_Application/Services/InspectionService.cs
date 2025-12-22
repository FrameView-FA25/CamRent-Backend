using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static CamRent_Application.DTOs.InspectionDTO;

namespace CamRent_Application.Services
{
	public class InspectionService : IInspectionService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public InspectionService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		// Row detail: used by /api/inspections/{id} for media upload/view
		public async Task<InspectionResponseDTO?> GetByIdAsync(Guid id)
		{
			var inspection = await _unitOfWork.Repository<Inspection>().GetByIdAsync(id);
			if (inspection == null) return null;

			var dto = _mapper.Map<InspectionResponseDTO>(inspection);

			var selections = (await _unitOfWork.Repository<InspectionMethodSelection>()
				.ListAsync(
					filter: s => s.InspectionId == id,
					include: q => q.Include(x => x.Method)
				)).ToList();
			dto.Methods = selections
				.Select(s => s.Method)
				.Where(m => m != null)
				.OrderBy(m => m.SortOrder)
				.Select(m => new InspectionChecklistDTO.InspectionMethodResponse
				{
					Id = m.Id,
					Code = m.Code,
					Name = m.Name,
					SortOrder = m.SortOrder,
					IsActive = m.IsActive
				}).ToList();

			var assets = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerId == inspection.Id && f.OwnerType == FileOwnerType.Inspection)).ToList();
			dto.Media = _mapper.Map<List<FileAssetDTO>>(assets);

			return dto;
		}
	}
}

