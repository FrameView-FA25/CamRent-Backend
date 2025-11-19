using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class ComboService : IComboService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IIndexingService _indexing;
		public ComboService(IUnitOfWork unitOfWork, IIndexingService indexing)
		{
			_unitOfWork = unitOfWork;
			_indexing = indexing;
		}

		public async Task<Guid> CreateAsync(string name, string? description, decimal? priceOverride)
		{
			var combo = new Combo { Id = Guid.NewGuid(), Name = name, Description = description, PriceOverride = priceOverride, CreatedAt = DateTime.UtcNow };
			await _unitOfWork.Repository<Combo>().AddAsync(combo);
			await _unitOfWork.Complete();
			_indexing.EnqueueUpsert("Combo", combo.Id);
			return combo.Id;
		}

		public async Task AddItemAsync(Guid comboId, Guid? cameraId, Guid? accessoryId)
		{
			if ((cameraId.HasValue && accessoryId.HasValue) || (!cameraId.HasValue && !accessoryId.HasValue))
				throw new ArgumentException("Provide exactly one of cameraId or accessoryId");
			var item = new ComboItem { Id = Guid.NewGuid(), ComboId = comboId, CameraId = cameraId, AccessoryId = accessoryId, CreatedAt = DateTime.UtcNow };
			await _unitOfWork.Repository<ComboItem>().AddAsync(item);
			await _unitOfWork.Complete();
			_indexing.EnqueueUpsert("Combo", comboId);
		}

		public async Task RemoveItemAsync(Guid comboItemId)
		{
			var item = await _unitOfWork.Repository<ComboItem>().GetByIdAsync(comboItemId) ?? throw new InvalidOperationException("Combo item not found");
			await _unitOfWork.Repository<ComboItem>().DeleteAsync(comboItemId);
			await _unitOfWork.Complete();
			_indexing.EnqueueUpsert("Combo", item.ComboId);
		}

		public async Task<(Guid id, string name, string? description, decimal? priceOverride, List<(Guid? cameraId, Guid? accessoryId)> items)> GetAsync(Guid comboId)
		{
			var combo = await _unitOfWork.Repository<Combo>().GetByIdAsync(comboId)
				?? throw new InvalidOperationException("Combo not found");
			var items = await _unitOfWork.Repository<ComboItem>().ListAsync(ci => ci.ComboId == comboId);
			return (combo.Id, combo.Name, combo.Description, combo.PriceOverride, items.Select(i => (i.CameraId, i.AccessoryId)).ToList());
		}
	}
}
