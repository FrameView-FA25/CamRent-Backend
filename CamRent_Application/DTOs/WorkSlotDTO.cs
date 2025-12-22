using CamRent_Domain.Entities;

namespace CamRent_Application.DTOs
{
	public class WorkSlotDTO
	{
		public class WorkSlotResponse
		{
			public Guid Id { get; set; }
			public int SlotIndex { get; set; }
			public TimeSpan StartTime { get; set; }
			public TimeSpan EndTime { get; set; }
			public bool IsActive { get; set; }
		}

		public class CreateWorkSlotRequest
		{
			public int SlotIndex { get; set; }
			public TimeSpan StartTime { get; set; }
			public TimeSpan EndTime { get; set; }
			public bool IsActive { get; set; } = true;
		}

		public class UpdateWorkSlotRequest
		{
			public TimeSpan? StartTime { get; set; }
			public TimeSpan? EndTime { get; set; }
			public bool? IsActive { get; set; }
		}
	}
}


