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

		public async Task<List<(Guid id, string name, Guid? parentId)>> ListAsync()
		{
			var list = await _unitOfWork.Repository<Category>().GetAllAsync();
			return list.Select(c => (c.Id, c.Name, c.ParentId)).ToList();
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
