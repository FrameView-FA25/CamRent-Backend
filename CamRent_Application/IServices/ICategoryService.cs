using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface ICategoryService
	{
		Task<Guid> CreateAsync(string name, Guid? parentId);
		Task<List<(Guid id, string name, Guid? parentId)>> ListAsync();
		Task DeleteAsync(Guid categoryId);
		Task LinkCameraAsync(Guid categoryId, Guid cameraId);
		Task LinkAccessoryAsync(Guid categoryId, Guid accessoryId);
	}
}
