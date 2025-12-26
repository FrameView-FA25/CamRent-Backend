using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static CamRent_Application.DTOs.ReviewDTO;

namespace CamRent_Application.Services
{
	public class ReviewService : IReviewService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public ReviewService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		// CREATE
		public async Task<Guid> CreateForCameraAsync(Guid authorUserId, Guid targetCameraId, int rating, string content)
		{
			var review = new Review
			{
				Id = Guid.NewGuid(),
				AuthorUserId = authorUserId,
				TargetCameraId = targetCameraId,
				Rating = rating,
				Content = content,
				Status = ReviewStatus.Pending,
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
				Status = ReviewStatus.Pending,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<Review>().AddAsync(review);
			await _unitOfWork.Complete();
			return review.Id;
		}

		// READ
		public async Task<ReviewResponseDTO?> GetByIdAsync(Guid reviewId)
		{
			var review = (await _unitOfWork.Repository<Review>().ListAsync(
				filter: r => r.Id == reviewId,
				include: q => q
					.Include(r => r.AuthorUser).ThenInclude(u => u.Avatar)
					.Include(r => r.TargetCamera)
					.Include(r => r.TargetAccessory)
					.Include(r => r.ReviewedByStaff)
			)).FirstOrDefault();

			if (review == null) return null;

			// Load media
			review.Media = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerType == FileOwnerType.Review && f.OwnerId == review.Id)).ToList();

			var dto = _mapper.Map<ReviewResponseDTO>(review);
			return dto;
		}

		public async Task<List<ReviewResponseDTO>> GetByCameraIdAsync(Guid cameraId, bool onlyApproved = true)
		{
			var reviews = await _unitOfWork.Repository<Review>().ListAsync(
				filter: r => r.TargetCameraId == cameraId && (!onlyApproved || r.Status == ReviewStatus.Approved),
				include: q => q
					.Include(r => r.AuthorUser).ThenInclude(u => u.Avatar)
					.Include(r => r.TargetCamera)
					.Include(r => r.ReviewedByStaff)
			);

			// Load media for each review
			foreach (var review in reviews)
			{
				review.Media = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerType == FileOwnerType.Review && f.OwnerId == review.Id)).ToList();
			}

			return _mapper.Map<List<ReviewResponseDTO>>(reviews);
		}

		public async Task<List<ReviewResponseDTO>> GetByAccessoryIdAsync(Guid accessoryId, bool onlyApproved = true)
		{
			var reviews = await _unitOfWork.Repository<Review>().ListAsync(
				filter: r => r.TargetAccessoryId == accessoryId && (!onlyApproved || r.Status == ReviewStatus.Approved),
				include: q => q
					.Include(r => r.AuthorUser).ThenInclude(u => u.Avatar)
					.Include(r => r.TargetAccessory)
					.Include(r => r.ReviewedByStaff)
			);

			// Load media for each review
			foreach (var review in reviews)
			{
				review.Media = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerType == FileOwnerType.Review && f.OwnerId == review.Id)).ToList();
			}

			return _mapper.Map<List<ReviewResponseDTO>>(reviews);
		}

		public async Task<List<ReviewResponseDTO>> GetByAuthorIdAsync(Guid authorUserId)
		{
			var reviews = await _unitOfWork.Repository<Review>().ListAsync(
				filter: r => r.AuthorUserId == authorUserId,
				include: q => q
					.Include(r => r.AuthorUser).ThenInclude(u => u.Avatar)
					.Include(r => r.TargetCamera)
					.Include(r => r.TargetAccessory)
					.Include(r => r.ReviewedByStaff)
			);

			// Load media for each review
			foreach (var review in reviews)
			{
				review.Media = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerType == FileOwnerType.Review && f.OwnerId == review.Id)).ToList();
			}

			return _mapper.Map<List<ReviewResponseDTO>>(reviews);
		}

		public async Task<List<ReviewResponseDTO>> GetPendingReviewsAsync()
		{
			var reviews = await _unitOfWork.Repository<Review>().ListAsync(
				filter: r => r.Status == ReviewStatus.Pending,
				include: q => q
					.Include(r => r.AuthorUser).ThenInclude(u => u.Avatar)
					.Include(r => r.TargetCamera)
					.Include(r => r.TargetAccessory)
					.Include(r => r.ReviewedByStaff)
			);

			// Load media for each review
			foreach (var review in reviews)
			{
				review.Media = (await _unitOfWork.Repository<FileAsset>()
					.ListAsync(f => f.OwnerType == FileOwnerType.Review && f.OwnerId == review.Id)).ToList();
			}

			return _mapper.Map<List<ReviewResponseDTO>>(reviews);
		}

		// UPDATE
		public async Task<bool> UpdateReviewAsync(Guid reviewId, Guid authorUserId, UpdateReviewRequestDTO request)
		{
			var review = await _unitOfWork.Repository<Review>().GetByIdAsync(reviewId);
			if (review == null || review.AuthorUserId != authorUserId)
				return false;

			// Only allow update if review is still pending
			if (review.Status != ReviewStatus.Pending)
				return false;

			if (request.Rating.HasValue)
				review.Rating = request.Rating.Value;

			if (!string.IsNullOrEmpty(request.Content))
				review.Content = request.Content;

			review.UpdatedAt = DateTime.UtcNow;
			await _unitOfWork.Repository<Review>().UpdateAsync(review);
			await _unitOfWork.Complete();
			return true;
		}

		public async Task<bool> ModerateReviewAsync(Guid reviewId, Guid staffId, ModerateReviewRequestDTO request)
		{
			var review = await _unitOfWork.Repository<Review>().GetByIdAsync(reviewId);
			if (review == null)
				return false;

			review.Status = request.Status;
			review.ReviewedByStaffId = staffId;
			review.ReviewedAt = DateTime.UtcNow;
			review.ModerationNotes = request.ModerationNotes;

			await _unitOfWork.Repository<Review>().UpdateAsync(review);
			await _unitOfWork.Complete();
			return true;
		}

		// DELETE
		public async Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdminOrStaff = false)
		{
			var review = await _unitOfWork.Repository<Review>().GetByIdAsync(reviewId);
			if (review == null)
				return false;

			// Only author can delete, or admin/staff
			if (!isAdminOrStaff && review.AuthorUserId != userId)
				return false;

			// Delete associated media files
			var mediaFiles = await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerType == FileOwnerType.Review && f.OwnerId == reviewId);

			foreach (var media in mediaFiles)
			{
				await _unitOfWork.Repository<FileAsset>().DeleteAsync(media.Id);
			}

			await _unitOfWork.Repository<Review>().DeleteAsync(reviewId);
			await _unitOfWork.Complete();
			return true;
		}
	}
}
