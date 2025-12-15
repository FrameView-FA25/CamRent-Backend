namespace CamRent_Application.DTOs
{
	public static class HomePageDTO
	{
		public sealed class CarouselItemResponse
		{
			public Guid Id { get; set; }
			public string Title { get; set; } = string.Empty;
			public string Content { get; set; } = string.Empty;
			public string? LinkUrl { get; set; }
			public int SortOrder { get; set; }
			public bool IsActive { get; set; }
			public string? ImageUrl { get; set; }
		}

		public sealed class ReorderCarouselItemRequest
		{
			public Guid Id { get; set; }
			public int SortOrder { get; set; }
		}
	}
}

