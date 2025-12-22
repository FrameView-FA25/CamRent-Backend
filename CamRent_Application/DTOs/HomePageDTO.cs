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

		public sealed class BlockResponse
		{
			public Guid Id { get; set; }
			public string Key { get; set; } = string.Empty;
			public string Title { get; set; } = string.Empty;
			public string Content { get; set; } = string.Empty;
			public int SortOrder { get; set; }
			public bool IsActive { get; set; }
			public string? ImageUrl { get; set; }
		}

		public sealed class HomePageFeedbackResponse
		{
			public Guid Id { get; set; }
			public int Rating { get; set; }
			public string Content { get; set; } = string.Empty;
			public DateTime CreatedAt { get; set; }

			public Guid AuthorUserId { get; set; }
			public string AuthorName { get; set; } = string.Empty;
			public string? AuthorAvatarUrl { get; set; }
		}
	}
}

