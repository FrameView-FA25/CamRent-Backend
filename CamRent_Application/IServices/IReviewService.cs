using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IReviewService
	{
		Task<Guid> CreateForCameraAsync(Guid authorUserId, Guid targetCameraId, int rating, string content);
		Task<Guid> CreateForAccessoryAsync(Guid authorUserId, Guid targetAccessoryId, int rating, string content);
	}
}
