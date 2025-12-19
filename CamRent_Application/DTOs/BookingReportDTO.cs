namespace CamRent_Application.DTOs
{
	public static class BookingReportDTO
	{
		public sealed class BookingReportResponse
		{
			public Guid Id { get; set; }
			public Guid BookingId { get; set; }
			public Guid ReporterUserId { get; set; }
			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Severity { get; set; } = "minor";
			public string Status { get; set; } = "open";
			public DateTime CreatedAt { get; set; }
			public List<string> ImageUrls { get; set; } = new();
		}
	}
}

