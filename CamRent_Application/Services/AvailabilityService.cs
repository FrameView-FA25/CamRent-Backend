using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System.Linq;

namespace CamRent_Application.Services
{
	public class AvailabilityService : IAvailabilityService
	{
		private readonly IUnitOfWork _unitOfWork;
		public AvailabilityService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> IsCameraAvailableAsync(Guid cameraId, DateTime start, DateTime end)
		{
			var overlapping = await _unitOfWork.Repository<Booking>().ListAsync(
				b => b.Status != BookingStatus.Cancelled
					&& b.Status != BookingStatus.Completed
					&& b.PickupAt < end
					&& b.ReturnAt > start
			);
			if (!overlapping.Any()) return true;
			var overlappingIds = overlapping.Select(b => b.Id).ToList();
			var conflicts = await _unitOfWork.Repository<BookingItem>().ListAsync(
				bi => bi.CameraId == cameraId && overlappingIds.Contains(bi.BookingId)
			);
			return !conflicts.Any();
		}

		public async Task<bool> IsAccessoryAvailableAsync(Guid accessoryId, DateTime start, DateTime end)
		{
			var overlapping = await _unitOfWork.Repository<Booking>().ListAsync(
				b => b.Status != BookingStatus.Cancelled
					&& b.Status != BookingStatus.Completed
					&& b.PickupAt < end
					&& b.ReturnAt > start
			);
			if (!overlapping.Any()) return true;
			var overlappingIds = overlapping.Select(b => b.Id).ToList();
			var conflicts = await _unitOfWork.Repository<BookingItem>().ListAsync(
				bi => bi.AccessoryId == accessoryId && overlappingIds.Contains(bi.BookingId)
			);
			return !conflicts.Any();
		}
	}
}
