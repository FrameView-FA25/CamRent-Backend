using System;
using System.Threading.Tasks;
using CamRent_Application.DTOs;

namespace CamRent_Application.IServices
{
	public interface IPricingService
	{
		Task<(decimal unitPricePerDay, decimal depositPerItem, decimal platformFeePercent)> GetCameraPricingAsync(Guid cameraId, int days);
		Task<(decimal unitPricePerDay, decimal depositPerItem, decimal platformFeePercent)> GetAccessoryPricingAsync(Guid accessoryId, int days);

		Task<PricingQuoteResult> QuoteBookingAsync(Guid bookingId, decimal? platformFeePercentOverride = null, decimal ownerShareRatio = 0.75m);
		decimal ComputeLateFee(decimal baseDailyRate, int lateDays);
		Task<DepositSettlement> ComputeSettlementAsync(Guid bookingId, int lateDays, decimal repairCost, int downtimeDays, decimal missingAccessoriesCost, decimal cleaningCost);
	}
}
