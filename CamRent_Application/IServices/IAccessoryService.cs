using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.AccessoryDTO;

namespace CamRent_Application.IServices
{
	public interface IAccessoryService
	{
		Task<List<AccessoryResponseDTO>> GetAllAccessoriesAsync();
		Task<AccessoryResponseDTO?> GetAccessoryByIdAsync(Guid accessoryId);
		Task<List<AccessoryResponseDTO>> GetAccessoriesByOwnerIdAsync(Guid userId);
		Task<int> CreateAccessoryAsync(Accessory accessory);
		Task<int> UpdateAccessoryAsync(Accessory accessory);
		Task<int> DeleteAccessoryAsync(Guid accessoryId);
	}
}
