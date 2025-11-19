using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
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

		public async Task<Guid> CreateInspectionAsync(InspectionRequest inspectionRequest, Guid staffId)
		{
			if (inspectionRequest == null)
				throw new ArgumentNullException(nameof(inspectionRequest));

			// Nếu là Booking/Verification mà không có Id tương ứng thì báo lỗi sớm
			if ((inspectionRequest.Type == InspectionType.Booking ||
				 inspectionRequest.Type == InspectionType.Verification)
				&& inspectionRequest.InspectionTypeId == null)
			{
				throw new ArgumentException("InspectionTypeId is required for this inspection type.");
			}

			// Map từ DTO sang entity bằng AutoMapper
			var inspection = _mapper.Map<Inspection>(inspectionRequest);

			inspection.CreatedAt = DateTime.UtcNow;
			inspection.CreatedByUserId = staffId;

			// CreatedAt thì nên để DbContext/SaveChanges xử lý (BaseEntity)
			await _unitOfWork.Repository<Inspection>().AddAsync(inspection);
			await _unitOfWork.Complete();

			return inspection.Id;
		}

		public async Task<List<InspectionResponseDTO>> GetByBookingAsync(Guid bookingId)
		{
			// Load inspections attached to the booking
			var inspections = (await _unitOfWork.Repository<Inspection>()
				.ListAsync(i => i.BookingId == bookingId)).ToList();

			var dtos = _mapper.Map<List<InspectionResponseDTO>>(inspections);

			// Populate Media for each inspection (mapping profile ignores Media)
			for (int idx = 0; idx < inspections.Count; idx++)
			{
				var inspection = inspections[idx];
				var dto = dtos[idx];

				var assets = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerId == inspection.Id && f.OwnerType == FileOwnerType.Inspection)).ToList();

				dto.Media = _mapper.Map<List<FileAssetDTO>>(assets);
			}

			return dtos;
		}

		// New: get inspections for a verification request and include media
		public async Task<List<InspectionResponseDTO>> GetByVerificationAsync(Guid verificationId)
		{
			var inspections = (await _unitOfWork.Repository<Inspection>()
				.ListAsync(i => i.VerificationId == verificationId)).ToList();

			var dtos = _mapper.Map<List<InspectionResponseDTO>>(inspections);

			// Populate Media for each inspection (mapping profile ignores Media)
			for (int idx = 0; idx < inspections.Count; idx++)
			{
				var inspection = inspections[idx];
				var dto = dtos[idx];

				var assets = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerId == inspection.Id && f.OwnerType == FileOwnerType.Inspection)).ToList();

				dto.Media = _mapper.Map<List<FileAssetDTO>>(assets);
			}

			return dtos;
		}
	}
}
