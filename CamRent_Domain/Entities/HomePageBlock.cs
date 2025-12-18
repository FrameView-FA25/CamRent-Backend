using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public sealed class HomePageBlock : BaseEntity
	{
		/// <summary>
		/// Key cố định để FE map section, ví dụ: hero_1, hero_2, testimonials.
		/// </summary>
		public string Key { get; set; } = string.Empty;

		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;

		public int SortOrder { get; set; } = 0;
		public bool IsActive { get; set; } = true;

		public Guid? ImageAssetId { get; set; }
		public FileAsset? ImageAsset { get; set; }
	}
}

