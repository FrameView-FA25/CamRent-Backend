using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public sealed class BookingIssueReport : BaseEntity
	{
		public Guid BookingId { get; set; }
		public Booking Booking { get; set; } = default!;

		public Guid ReporterUserId { get; set; }
		public User ReporterUser { get; set; } = default!;

		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// minor | major | critical (tuỳ FE gửi)
		/// </summary>
		public string Severity { get; set; } = "minor";

		/// <summary>
		/// pending | under_review | resolved | rejected
		/// </summary>
		public string Status { get; set; } = "pending";

		public Guid? HandledByStaffId { get; set; }
		public User? HandledByStaff { get; set; }
		public DateTime? HandledAt { get; set; }
		public string? HandlerNote { get; set; }
	}
}

