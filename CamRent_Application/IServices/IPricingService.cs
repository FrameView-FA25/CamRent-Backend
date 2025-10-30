using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IPricingService
	{
		Task<(decimal unitPricePerDay, decimal depositPerItem, decimal platformFeePercent)> GetCameraPricingAsync(Guid cameraId, int days);
		Task<(decimal unitPricePerDay, decimal depositPerItem, decimal platformFeePercent)> GetAccessoryPricingAsync(Guid accessoryId, int days);
	}
}
