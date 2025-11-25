using CamRent_Domain.Entities;
using CamRent_Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.AccessoryDTO;

namespace CamRent_Application.IServices
{
	public interface IAccessoryService
	{
		Task<List<AccessoryResponseDTO>> GetAllAccessoriesAsync();
		Task<AccessoryResponseDTO?> GetAccessoryByIdAsync(Guid accessoryId);
		Task<List<AccessoryResponseDTO>> GetAccessoriesByOwnerIdAsync(Guid userId);
		Task<AccessoryHistoryDTO> GetHistoryForQrAsync(Guid accessoryId);
		Task<int> CreateAccessoryAsync(Accessory accessory);
		Task<int> UpdateAccessoryAsync(UpdateAccessoryRequest request, Guid userId);
		Task<int> DeleteAccessoryAsync(Guid accessoryId);
	}
}
