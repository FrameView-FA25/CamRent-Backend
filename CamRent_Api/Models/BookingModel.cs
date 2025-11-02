namespace CamRent_Api.Models
{
	public class BookingModel
	{
		public class CreateBookingRequest
		{
			public Guid RenterId { get; set; }
			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
		}

		public class AddItemRequest
		{
			public Guid? CameraId { get; set; }
			public Guid? AccessoryId { get; set; }
			public int Quantity { get; set; }
			public decimal UnitPrice { get; set; }
			public decimal DepositAmount { get; set; }
		}

		public class UpdateTimesRequest
		{
			public DateTime PickupAt { get; set; }
			public DateTime ReturnAt { get; set; }
		}

		public class SettlementRequest
		{
			public int LateDays { get; set; }
			public decimal RepairCost { get; set; }
			public int DowntimeDays { get; set; }
			public decimal MissingAccessoriesCost { get; set; }
			public decimal CleaningCost { get; set; }
		}
	}
}
