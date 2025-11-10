using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface ICategoryService
	{
		Task<Guid> CreateAsync(string name, Guid? parentId);
		Task<(IEnumerable<(Guid id, string name, Guid? parentId)> items, int total)> ListAsync(string? search, string? sort = "name", bool desc = false, int page = 1, int pageSize = 20);
		Task DeleteAsync(Guid categoryId);
		Task LinkCameraAsync(Guid categoryId, Guid cameraId);
		Task LinkAccessoryAsync(Guid categoryId, Guid accessoryId);
	}
}
