using System;
using System.Collections.Generic;
using CamRent_Domain.Common;
using static CamRent_Application.DTOs.InspectionFormDTO;

namespace CamRent_Application.DTOs
{
	public class CameraHistoryDTO
	{
		public CameraDTO.CameraResponseDTO Camera { get; set; } = default!;
		public List<CameraBookingHistoryItem> Bookings { get; set; } = new();
		public List<InspectionFormSummaryResponse> InspectionForms { get; set; } = new();
	}

	public class CameraBookingHistoryItem
	{
		public Guid BookingId { get; set; }
		public DateTime PickupAt { get; set; }
		public DateTime ReturnAt { get; set; }
		public BookingStatus Status { get; set; }
		public string StatusText { get; set; } = string.Empty;
		public string? RenterName { get; set; }
	}
}

