using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace CamRent_Domain.Entities
{
    public class Review : BaseEntity
    {
        public Guid AuthorUserId { get; set; }
        public User AuthorUser { get; set; } = default!;

        public Guid? TargetCameraId { get; set; }
        public Camera? TargetCamera { get; set; }

        public Guid? TargetAccessoryId { get; set; }
        public Accessory? TargetAccessory { get; set; }

        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;

        // Quản lý bởi staff
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
        public Guid? ReviewedByStaffId { get; set; }
        public User? ReviewedByStaff { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ModerationNotes { get; set; }

		[NotMapped]
		public ICollection<FileAsset> Media { get; set; } = new List<FileAsset>();
	}
}