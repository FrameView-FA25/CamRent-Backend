using AutoMapper;
using CamRent_Application.DTOs;
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
		public CameraService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}
		public async Task<int> CreateAsync(Camera camera)
		{
			await _unitOfWork.Repository<Camera>().AddAsync(camera);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<int> DeleteAsync(Guid id)
		{
			await _unitOfWork.Repository<Camera>().DeleteAsync(id);
			var result = await _unitOfWork.Complete();
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

		public async Task<int> UpdateAsync(Camera camera)
		{
			await _unitOfWork.Repository<Camera>().UpdateAsync(camera);
			var result = await _unitOfWork.Complete();
			return result;
		}
	}
}
