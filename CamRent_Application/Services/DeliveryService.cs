using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class DeliveryService : IDeliveryService
	{
		private readonly IUnitOfWork _unitOfWork;
		public DeliveryService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> CreateTaskAsync(Guid bookingId, Guid? assigneeUserId, string? trackingCode, string? notes, decimal? deliveryFee)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var task = new DeliveryTask
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				AssigneeUserId = assigneeUserId,
				Status = DeliveryTaskStatus.Assigned,
				TrackingCode = trackingCode,
				Notes = notes,
				DeliveryFee = deliveryFee,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<DeliveryTask>().AddAsync(task);
			await _unitOfWork.Complete();
			return task.Id;
		}

		public async Task UpdateStatusAsync(Guid taskId, DeliveryTaskStatus status, DateTime? whenUtc = null)
		{
			var task = await _unitOfWork.Repository<DeliveryTask>().GetByIdAsync(taskId)
				?? throw new InvalidOperationException("Delivery task not found");
			task.Status = status;
			var t = whenUtc ?? DateTime.UtcNow;
			if (status == DeliveryTaskStatus.InTransit) task.PickedUpAt = t;
			if (status == DeliveryTaskStatus.Delivered) task.DeliveredAt = t;
			await _unitOfWork.Repository<DeliveryTask>().UpdateAsync(task);
			await _unitOfWork.Complete();
		}
	}
}
