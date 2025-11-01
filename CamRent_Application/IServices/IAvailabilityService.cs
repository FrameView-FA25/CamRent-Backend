using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IAvailabilityService
	{
		Task<bool> IsCameraAvailableAsync(Guid cameraId, DateTime start, DateTime end);
		Task<bool> IsAccessoryAvailableAsync(Guid accessoryId, DateTime start, DateTime end);
	}
}
