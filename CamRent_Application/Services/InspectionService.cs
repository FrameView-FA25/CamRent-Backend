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
			if (inspectionRequest.Type == InspectionType.Booking)
			{
				// Kiểm tra Booking tồn tại
				var booking = await _unitOfWork.Repository<Booking>()
					.GetByIdAsync(inspectionRequest.InspectionTypeId!.Value);
				if (booking == null)
				{
					throw new InvalidOperationException("Booking not found for the given InspectionTypeId.");
				}
			}
			else if (inspectionRequest.Type == InspectionType.Verification)
			{
				// Kiểm tra Verification tồn tại
				var verification = await _unitOfWork.Repository<VerificationRequest>()
					.GetByIdAsync(inspectionRequest.InspectionTypeId!.Value);
				if (verification == null)
				{
					throw new InvalidOperationException("VerificationRequest not found for the given InspectionTypeId.");
				}
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

		// New: get detail by id
		public async Task<InspectionResponseDTO?> GetByIdAsync(Guid id)
		{
			var inspection = await _unitOfWork.Repository<Inspection>().GetByIdAsync(id);
			if (inspection == null) return null;

			var dto = _mapper.Map<InspectionResponseDTO>(inspection);

			var assets = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerId == inspection.Id && f.OwnerType == FileOwnerType.Inspection)).ToList();

			dto.Media = _mapper.Map<List<FileAssetDTO>>(assets);
			return dto;
		}

		// New: update inspection
		public async Task<int> UpdateInspectionAsync(Guid id, InspectionRequest request, Guid staffId)
		{
			var inspection = await _unitOfWork.Repository<Inspection>().GetByIdAsync(id);
			if (inspection == null) return 0;

			_mapper.Map(request, inspection);

			// handle item id + type -> set CameraId / AccessoryId
			if (request.ItemType != null)
			{
				if (request.ItemType == ItemType.Camera)
				{
					inspection.CameraId = request.ItemId;
					inspection.AccessoryId = null;
				}
				else if (request.ItemType == ItemType.Accessory)
				{
					inspection.AccessoryId = request.ItemId;
					inspection.CameraId = null;
				}
				else
				{
					inspection.CameraId = null;
					inspection.AccessoryId = null;
				}
			}

			// Use CreatedByUserId to track staff who performed update
			inspection.UpdatedByUserId = staffId;
			inspection.UpdatedAt = DateTime.UtcNow;

			await _unitOfWork.Repository<Inspection>().UpdateAsync(inspection);
			return await _unitOfWork.Complete();
		}

		

		// New: delete inspection
		public async Task<int> DeleteInspectionAsync(Guid id)
		{
			var inspection = await _unitOfWork.Repository<Inspection>().GetByIdAsync(id);
			if (inspection == null) return 0;

			// delete file assets of this inspection
			var assets = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerId == inspection.Id && f.OwnerType == FileOwnerType.Inspection)).ToList();

			foreach (var asset in assets)
			{
				await _unitOfWork.Repository<FileAsset>().DeleteAsync(asset.Id);
			}

			await _unitOfWork.Repository<Inspection>().DeleteAsync(id);
			return await _unitOfWork.Complete();
		}

		public async Task<int> ApproveInspectionAsync(Guid id, Guid managerId, bool pass)
		{
			var inspectionTask = await _unitOfWork.Repository<Inspection>().GetByIdAsync(id);
			inspectionTask.ManagerId = managerId;
			inspectionTask.Passed = pass;
			inspectionTask.PerformedAt = DateTime.UtcNow;
			await _unitOfWork.Repository<Inspection>().UpdateAsync(inspectionTask);
			var result = await _unitOfWork.Complete();
			return result;
		}
	}
}
