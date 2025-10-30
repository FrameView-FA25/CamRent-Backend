using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class PricingService : IPricingService
	{
		private readonly IUnitOfWork _unitOfWork;
		public PricingService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<(decimal unitPricePerDay, decimal depositPerItem, decimal platformFeePercent)> GetCameraPricingAsync(Guid cameraId, int days)
		{
			var camera = await _unitOfWork.Repository<Camera>().GetByIdAsync(cameraId)
				?? throw new InvalidOperationException("Camera not found");
			var unit = ApplyTier(camera.BaseDailyRate, days);
			var deposit = ClampDeposit(camera.EstimatedValueVnd * (camera.DepositPercent / 100m), camera.DepositCapMinVnd, camera.DepositCapMaxVnd);
			return (unit, deposit, camera.PlatformFeePercent);
		}

		public async Task<(decimal unitPricePerDay, decimal depositPerItem, decimal platformFeePercent)> GetAccessoryPricingAsync(Guid accessoryId, int days)
		{
			var accessory = await _unitOfWork.Repository<Accessory>().GetByIdAsync(accessoryId)
				?? throw new InvalidOperationException("Accessory not found");
			var unit = ApplyTier(accessory.BaseDailyRate, days);
			var deposit = ClampDeposit(accessory.EstimatedValueVnd * (accessory.DepositPercent / 100m), accessory.DepositCapMinVnd, accessory.DepositCapMaxVnd);
			return (unit, deposit, accessory.PlatformFeePercent);
		}

		private static decimal ApplyTier(decimal baseDaily, int days)
		{
			if (days >= 28) return baseDaily * 0.60m;
			if (days >= 7) return baseDaily * 0.85m;
			return baseDaily;
		}

		private static decimal ClampDeposit(decimal value, decimal? min, decimal? max)
		{
			var lower = min ?? 5_000_000m; // default example caps per spec
			var upper = max ?? 30_000_000m;
			if (value < lower) return lower;
			if (value > upper) return upper;
			return value;
		}
	}
}
