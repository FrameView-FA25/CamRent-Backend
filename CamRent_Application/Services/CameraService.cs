using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using CamRent_Application.Interfaces;
using CamRent_Application.DTOs;
using AutoMapper;

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
		public async Task<int> AddAsync(Camera camera)
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
			var listCamera = await _unitOfWork.Repository<Camera>().GetAllAsync();
			var result = _mapper.Map<List<CameraResponseDTO>>(listCamera);
			return result;
		}

		public async Task<CameraResponseDTO?> GetByIdAsync(Guid id)
		{
			var camera = await _unitOfWork.Repository<Camera>().GetByIdAsync(id);
			var cameraResponse = _mapper.Map<CameraResponseDTO>(camera);
			return cameraResponse;
		}

		public Task<IEnumerable<CameraResponseDTO>> ListAsync()
		{
			throw new NotImplementedException();
		}

		public async Task<int> UpdateAsync(Camera camera)
		{
			await _unitOfWork.Repository<Camera>().UpdateAsync(camera);
			var result = await _unitOfWork.Complete();
			return result;
		}
	}
}
