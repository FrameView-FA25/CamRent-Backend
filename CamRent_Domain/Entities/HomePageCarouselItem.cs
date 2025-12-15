using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public sealed class HomePageCarouselItem : BaseEntity
	{
		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string? LinkUrl { get; set; }

		public int SortOrder { get; set; } = 0;
		public bool IsActive { get; set; } = true;

		public Guid? ImageAssetId { get; set; }
		public FileAsset? ImageAsset { get; set; }
	}
}

