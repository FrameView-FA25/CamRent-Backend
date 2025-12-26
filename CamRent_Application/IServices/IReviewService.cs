using CamRent_Application.DTOs;
using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IReviewService
	{
		// CREATE
		Task<Guid> CreateForCameraAsync(Guid authorUserId, Guid targetCameraId, int rating, string content);
		Task<Guid> CreateForAccessoryAsync(Guid authorUserId, Guid targetAccessoryId, int rating, string content);

		// READ
		Task<ReviewResponseDTO?> GetByIdAsync(Guid reviewId);
		Task<List<ReviewResponseDTO>> GetByCameraIdAsync(Guid cameraId, bool onlyApproved = true);
		Task<List<ReviewResponseDTO>> GetByAccessoryIdAsync(Guid accessoryId, bool onlyApproved = true);
		Task<List<ReviewResponseDTO>> GetByAuthorIdAsync(Guid authorUserId);
		Task<List<ReviewResponseDTO>> GetPendingReviewsAsync(); // For Staff/Admin moderation

		// UPDATE
		Task<bool> UpdateReviewAsync(Guid reviewId, Guid authorUserId, ReviewDTO.UpdateReviewRequestDTO request);
		Task<bool> ModerateReviewAsync(Guid reviewId, Guid staffId, ReviewDTO.ModerateReviewRequestDTO request);

		// DELETE
		Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdminOrStaff = false);
	}
}
