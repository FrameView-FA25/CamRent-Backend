using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using static CamRent_Application.DTOs.CameraDTO;

namespace CamRent_Application.IServices
{
	public interface ICameraService
	{
		Task<CameraResponseDTO?> GetByIdAsync(Guid id);
		Task<List<CameraResponseDTO>> GetAllAsync();
		Task<List<CameraResponseDTO>> GetCamerasByOwnerIdAsync(Guid userId);
		Task<List<CameraResponseDTO>> GetByBranchManagerAsync(Guid managerId);
		Task<CameraHistoryDTO> GetHistoryForQrAsync(Guid cameraId);
		Task<int> CreateAsync(Camera camera);
		Task<int> UpdateAsync(UpdateCameraRequest request, Guid userId);
		Task<int> DeleteAsync(Guid id);
	}
}
