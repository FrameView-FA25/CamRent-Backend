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

		public sealed class BookingReportDeviceBrief
		{
			public string ItemType { get; set; } = string.Empty; // camera|accessory|combo
			public Guid ItemId { get; set; }
			public string Name { get; set; } = string.Empty;
			public string? SerialNumber { get; set; }
		}

		public sealed class BookingIssueReportStaffListItem
		{
			public Guid Id { get; set; }
			public Guid BookingId { get; set; }
			public string? BookingCode { get; set; }
			public DateTime CreatedAt { get; set; }
			public string Title { get; set; } = string.Empty;
			public string Severity { get; set; } = "minor";
			public string Status { get; set; } = "open";
			public string StatusText { get; set; } = string.Empty;
			public string ReporterName { get; set; } = string.Empty;
			public List<BookingReportDeviceBrief> Devices { get; set; } = new();
		}

		public sealed class BookingIssueReportStaffDetail
		{
			public Guid Id { get; set; }
			public Guid BookingId { get; set; }
			public string? BookingCode { get; set; }
			public DateTime CreatedAt { get; set; }

			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Severity { get; set; } = "minor";
			public string Status { get; set; } = "open";
			public string StatusText { get; set; } = string.Empty;

			public Guid ReporterUserId { get; set; }
			public string ReporterName { get; set; } = string.Empty;

			public List<BookingReportDeviceBrief> Devices { get; set; } = new();
			public List<string> ImageUrls { get; set; } = new();
		}
	}
}

