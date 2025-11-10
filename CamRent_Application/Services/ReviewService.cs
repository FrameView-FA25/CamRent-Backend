using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class ReviewService : IReviewService
	{
		private readonly IUnitOfWork _unitOfWork;
		public ReviewService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> CreateForCameraAsync(Guid authorUserId, Guid targetCameraId, int rating, string content)
		{
			var review = new Review
			{
				Id = Guid.NewGuid(),
				AuthorUserId = authorUserId,
				TargetCameraId = targetCameraId,
				Rating = rating,
				Content = content,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<Review>().AddAsync(review);
			await _unitOfWork.Complete();
			return review.Id;
		}

		public async Task<Guid> CreateForAccessoryAsync(Guid authorUserId, Guid targetAccessoryId, int rating, string content)
		{
			var review = new Review
			{
				Id = Guid.NewGuid(),
				AuthorUserId = authorUserId,
				TargetAccessoryId = targetAccessoryId,
				Rating = rating,
				Content = content,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<Review>().AddAsync(review);
			await _unitOfWork.Complete();
			return review.Id;
		}
	}
}
