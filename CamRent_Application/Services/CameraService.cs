using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static CamRent_Application.DTOs.CameraDTO;

namespace CamRent_Application.Services
{
	public class CameraService : ICameraService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IIndexingService _indexing;
		public CameraService(IUnitOfWork unitOfWork, IMapper mapper, IIndexingService indexing)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_indexing = indexing;
		}
		public async Task<int> CreateAsync(Camera camera)
		{
			await _unitOfWork.Repository<Camera>().AddAsync(camera);
			var result = await _unitOfWork.Complete();
			if (result > 0) _indexing.EnqueueUpsert("Camera", camera.Id);
			return result;
		}

		public async Task<int> DeleteAsync(Guid id)
		{
			await _unitOfWork.Repository<Camera>().DeleteAsync(id);
			var result = await _unitOfWork.Complete();
			if (result > 0) _indexing.EnqueueDelete("Camera", id);
			return result;
		}

		public async Task<List<CameraResponseDTO>> GetAllAsync()
		{
			var listCamera = await _unitOfWork.Repository<Camera>().ListAsync(include: c => c.Include(c =>c.Branch));
			foreach (var camera in listCamera)
			{
				camera.Media = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == camera.Id)).ToList();
			}
			var result = _mapper.Map<List<CameraResponseDTO>>(listCamera);
			return result;
		}

		public async Task<CameraResponseDTO?> GetByIdAsync(Guid id)
		{
			var camera = (await _unitOfWork.Repository<Camera>().ListAsync(filter: c => c.Id == id, include: c => c.Include(c => c.Branch))).FirstOrDefault();
			camera!.Media = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == camera.Id)).ToList();
			var cameraResponse = _mapper.Map<CameraResponseDTO>(camera);
			return cameraResponse;
		}

		public async Task<List<CameraResponseDTO>> GetCamerasByOwnerIdAsync(Guid userId)
		{
			var cameras = await _unitOfWork.Repository<Camera>().ListAsync(
				filter: c => c.OwnerUserId == userId,
				include: c => c.Include(c => c.Branch)
			);
			foreach (var camera in cameras)
			{
				camera.Media = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == camera.Id)).ToList();
			}
			return _mapper.Map<List<CameraResponseDTO>>(cameras);
		}

		public async Task<List<CameraResponseDTO>> GetByBranchManagerAsync(Guid managerId)
		{
			var cameras = await _unitOfWork.Repository<Camera>()
				.ListAsync(
					filter: c => c.Branch.ManagerId == managerId,
					include: c => c.Include(c => c.Branch));
			foreach (var camera in cameras)
			{
				camera.Media = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == camera.Id)).ToList();
			}
			return _mapper.Map<List<CameraResponseDTO>>(cameras);
		}

		public async Task<CameraHistoryDTO> GetHistoryForQrAsync(Guid cameraId)
		{
			// Camera
			var camera = (await _unitOfWork.Repository<Camera>()
				.ListAsync(filter: c => c.Id == cameraId,
					include: c => c.Include(c => c.Branch)))
				.FirstOrDefault() ?? throw new InvalidOperationException("Camera not found");

			camera.Media = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == camera.Id)).ToList();

			var cameraDto = _mapper.Map<CameraResponseDTO>(camera);

			// Booking history (simple)
			var bookingItems = await _unitOfWork.Repository<BookingItem>()
				.ListAsync(bi => bi.CameraId == cameraId,
					include: q => q
						.Include(bi => bi.Booking)!.ThenInclude(b => b.Renter));

			var bookingHistory = bookingItems
				.Where(bi => bi.Booking != null)
				.Select(bi => bi.Booking!)
				.Distinct()
				.OrderByDescending(b => b.PickupAt)
				.Take(10)
				.Select(b => new CameraBookingHistoryItem
				{
					BookingId = b.Id,
					PickupAt = b.PickupAt,
					ReturnAt = b.ReturnAt,
					Status = b.Status,
					StatusText = b.Status.GetDisplayName(),
					RenterName = b.Renter?.FullName
				})
				.ToList();

			// Inspections liên quan tới camera
			var inspections = await _unitOfWork.Repository<Inspection>()
				.ListAsync(i => i.ItemId == cameraId && i.ItemType == ItemType.Camera);
			var inspectionDtos = _mapper.Map<List<InspectionDTO.InspectionResponseDTO>>(inspections);

			return new CameraHistoryDTO
			{
				Camera = cameraDto,
				Bookings = bookingHistory,
				Inspections = inspectionDtos
			};
		}

		public async Task<int> UpdateAsync(Camera camera)
		{
			await _unitOfWork.Repository<Camera>().UpdateAsync(camera);
			var result = await _unitOfWork.Complete();
			if (result > 0) _indexing.EnqueueUpsert("Camera", camera.Id);
			return result;
		}
	}
}
