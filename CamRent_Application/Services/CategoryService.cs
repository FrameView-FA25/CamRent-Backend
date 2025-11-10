using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class CategoryService : ICategoryService
	{
		private readonly IUnitOfWork _unitOfWork;
		public CategoryService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> CreateAsync(string name, Guid? parentId)
		{
			var cat = new Category { Id = Guid.NewGuid(), Name = name, ParentId = parentId, CreatedAt = DateTime.UtcNow };
			await _unitOfWork.Repository<Category>().AddAsync(cat);
			await _unitOfWork.Complete();
			return cat.Id;
		}

		public async Task<(IEnumerable<(Guid id, string name, Guid? parentId)> items, int total)> ListAsync(string? search, string? sort = "name", bool desc = false, int page = 1, int pageSize = 20)
		{
			var all = await _unitOfWork.Repository<Category>().ListAsync(
				filter: string.IsNullOrWhiteSpace(search) ? null : c => c.Name.ToLower().Contains(search!.ToLower())
			);
			IEnumerable<Category> ordered = sort?.ToLower() == "created"
				? (desc ? all.OrderByDescending(c => c.CreatedAt) : all.OrderBy(c => c.CreatedAt))
				: (desc ? all.OrderByDescending(c => c.Name) : all.OrderBy(c => c.Name));
			int total = ordered.Count();
			var pageItems = ordered.Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize)).Take(Math.Max(1, pageSize))
				.Select(c => (c.Id, c.Name, c.ParentId));
			return (pageItems, total);
		}

		public async Task DeleteAsync(Guid categoryId)
		{
			await _unitOfWork.Repository<Category>().DeleteAsync(categoryId);
			await _unitOfWork.Complete();
		}

		public async Task LinkCameraAsync(Guid categoryId, Guid cameraId)
		{
			var link = new DeviceCategoryLink { Id = Guid.NewGuid(), CategoryId = categoryId, CameraId = cameraId, CreatedAt = DateTime.UtcNow };
			await _unitOfWork.Repository<DeviceCategoryLink>().AddAsync(link);
			await _unitOfWork.Complete();
		}

		public async Task LinkAccessoryAsync(Guid categoryId, Guid accessoryId)
		{
			var link = new DeviceCategoryLink { Id = Guid.NewGuid(), CategoryId = categoryId, AccessoryId = accessoryId, CreatedAt = DateTime.UtcNow };
			await _unitOfWork.Repository<DeviceCategoryLink>().AddAsync(link);
			await _unitOfWork.Complete();
		}
	}
}
