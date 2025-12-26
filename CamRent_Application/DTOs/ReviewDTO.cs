using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CamRent_Application.DTOs
{
	public class ReviewDTO
	{
		public class ReviewResponseDTO
		{
			public Guid Id { get; set; }
			public Guid AuthorUserId { get; set; }
			public string AuthorName { get; set; } = string.Empty;
			public string? AuthorAvatarUrl { get; set; }

			public Guid? TargetCameraId { get; set; }
			public string? TargetCameraName { get; set; }
			public Guid? TargetAccessoryId { get; set; }
			public string? TargetAccessoryName { get; set; }

			public int Rating { get; set; }
			public string Content { get; set; } = string.Empty;

			public ReviewStatus Status { get; set; }
			public Guid? ReviewedByStaffId { get; set; }
			public string? ReviewedByStaffName { get; set; }
			public DateTime? ReviewedAt { get; set; }
			public string? ModerationNotes { get; set; }

			public DateTime CreatedAt { get; set; }
			public DateTime? UpdatedAt { get; set; }

			public List<FileAssetDTO>? Media { get; set; }
		}

		public class CreateReviewRequestDTO
		{
			public Guid? TargetCameraId { get; set; }
			public Guid? TargetAccessoryId { get; set; }
			[Required]
			[Range(1, 5)]
			public int Rating { get; set; }
			[Required]
			[MinLength(1)]
			public string Content { get; set; } = string.Empty;
		}

		public class UpdateReviewRequestDTO
		{
			[Range(1, 5)]
			public int? Rating { get; set; }
			[MinLength(1)]
			public string? Content { get; set; }
		}

		public class ModerateReviewRequestDTO
		{
			[Required]
			public ReviewStatus Status { get; set; }
			public string? ModerationNotes { get; set; }
		}
	}
}

