using System;
using System.Threading.Tasks;
using CamRent_Domain.Common;

namespace CamRent_Application.IServices
{
	public interface IDeliveryService
	{
		Task<Guid> CreateTaskAsync(Guid bookingId, Guid? assigneeUserId, string? trackingCode, string? notes, decimal? deliveryFee);
		Task UpdateStatusAsync(Guid taskId, DeliveryTaskStatus status, DateTime? whenUtc = null);
	}
}
