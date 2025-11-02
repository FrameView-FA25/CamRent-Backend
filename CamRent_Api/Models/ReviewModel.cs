using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class ReviewModel
	{
		public class CreateCameraReviewRequest { [Required] public Guid AuthorUserId { get; set; } [Required] public Guid TargetCameraId { get; set; } [Range(1,5)] public int Rating { get; set; } [Required, MinLength(1)] public string Content { get; set; } = string.Empty; }
		public class CreateAccessoryReviewRequest { [Required] public Guid AuthorUserId { get; set; } [Required] public Guid TargetAccessoryId { get; set; } [Range(1,5)] public int Rating { get; set; } [Required, MinLength(1)] public string Content { get; set; } = string.Empty; }
	}
}
