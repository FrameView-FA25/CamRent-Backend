using System;
using System.Collections.Generic;

namespace CamRent_Application.DTOs
{
	public class DisputeDTO
	{
		public class DisputeResponse
		{
			public Guid Id { get; set; }
			public Guid BookingId { get; set; }
			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Severity { get; set; } = "minor";
			public string Status { get; set; } = "open";
			public decimal TotalAmount { get; set; }
			public List<DisputeItemResponse> Items { get; set; } = new();
		}

		public class DisputeItemResponse
		{
			public Guid Id { get; set; }
			public string Type { get; set; } = string.Empty;
			public decimal Amount { get; set; }
			public string? Notes { get; set; }
		}
	}
}

