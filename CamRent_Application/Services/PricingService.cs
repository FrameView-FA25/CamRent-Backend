using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Application.DTOs;
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

		public async Task<PricingQuoteResult> QuoteBookingAsync(Guid bookingId, decimal? platformFeePercentOverride = null, decimal ownerShareRatio = 0.75m)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var items = await _unitOfWork.Repository<BookingItem>().ListAsync(bi => bi.BookingId == bookingId);
			int days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));

			decimal rentalTotal = 0m;
			decimal depositTotal = 0m;
			decimal platformFee = 0m;

			foreach (var i in items)
			{
				var itemTotal = i.UnitPrice * days;
				rentalTotal += itemTotal;
				depositTotal += i.DepositAmount;

				var pf = platformFeePercentOverride;
				if (pf == null)
				{
					if (i.CameraId.HasValue)
					{
						var cam = await _unitOfWork.Repository<Camera>().GetByIdAsync(i.CameraId.Value);
						pf = cam?.PlatformFeePercent ?? 0m;
					}
					else if (i.AccessoryId.HasValue)
					{
						var acc = await _unitOfWork.Repository<Accessory>().GetByIdAsync(i.AccessoryId.Value);
						pf = acc?.PlatformFeePercent ?? 0m;
					}
				}
				platformFee += itemTotal * ((pf ?? 0m) / 100m);
			}

			var net = rentalTotal - platformFee;
			var ownerShare = net * ownerShareRatio;
			var platformNet = net - ownerShare;

			return new PricingQuoteResult
			{
				BookingId = bookingId,
				Days = days,
				RentalTotal = rentalTotal,
				DepositTotal = depositTotal,
				PlatformFee = platformFee,
				NetRevenue = net,
				OwnerShare = ownerShare,
				PlatformNet = platformNet
			};
		}

		public decimal ComputeLateFee(decimal baseDailyRate, int lateDays)
		{
			if (lateDays <= 0) return 0m;
			if (lateDays <= 3) return lateDays * 1.5m * baseDailyRate;
			return (3 * 1.5m * baseDailyRate) + ((lateDays - 3) * 2.0m * baseDailyRate);
		}

		public async Task<DepositSettlement> ComputeSettlementAsync(Guid bookingId, int lateDays, decimal repairCost, int downtimeDays, decimal missingAccessoriesCost, decimal cleaningCost)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var items = await _unitOfWork.Repository<BookingItem>().ListAsync(bi => bi.BookingId == bookingId);
			int days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));

			decimal rBasePerDay = items.Sum(i => i.UnitPrice); // dùng unit price ngày
			decimal lateFee = ComputeLateFee(rBasePerDay, lateDays);
			decimal downtimeFee = 0.5m * rBasePerDay * Math.Max(0, downtimeDays);
			decimal depositCollected = items.Sum(i => i.DepositAmount);

			decimal totalDeductions = lateFee + repairCost + downtimeFee + missingAccessoriesCost + cleaningCost;
			decimal refund = Math.Max(0, depositCollected - totalDeductions);
			decimal extra = Math.Max(0, totalDeductions - depositCollected);

			return new DepositSettlement
			{
				BookingId = bookingId,
				LateDays = lateDays,
				DowntimeDays = downtimeDays,
				LateFee = lateFee,
				RepairCost = repairCost,
				DowntimeFee = downtimeFee,
				MissingAccessoriesCost = missingAccessoriesCost,
				CleaningCost = cleaningCost,
				TotalDeductions = totalDeductions,
				DepositCollected = depositCollected,
				DepositRefund = refund,
				ExtraDueFromCustomer = extra
			};
		}

		private static decimal ApplyTier(decimal baseDaily, int days)
		{
			if (days >= 28) return baseDaily * 0.60m;
			if (days >= 7) return baseDaily * 0.85m;
			return baseDaily;
		}

		private static decimal ClampDeposit(decimal value, decimal? min, decimal? max)
		{
			var lower = min ?? 5_000_000m; // default caps (can be configured)
			var upper = max ?? 30_000_000m;
			if (value < lower) return lower;
			if (value > upper) return upper;
			return value;
		}
	}
}
