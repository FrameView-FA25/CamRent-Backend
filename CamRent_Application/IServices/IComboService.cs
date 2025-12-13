using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IComboService
	{
		Task<Guid> CreateAsync(string name, string? description, decimal? priceOverride);
		Task AddItemAsync(Guid comboId, Guid? cameraId, Guid? accessoryId);
		Task RemoveItemAsync(Guid comboItemId);
		Task<(Guid id, string name, string? description, decimal? priceOverride, List<(Guid? cameraId, Guid? accessoryId)> items)> GetAsync(Guid comboId);
	}
}
