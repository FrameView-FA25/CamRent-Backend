using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class BookingModel
	{
		public class CreateBookingRequest
		{
			[Required]
			public Guid RenterId { get; set; }
			[Required]
			public DateTime PickupAt { get; set; }
			[Required]
			public DateTime ReturnAt { get; set; }
		}

		public class AddItemRequest
		{
			public Guid? CameraId { get; set; }
			public Guid? AccessoryId { get; set; }
			[Range(1, int.MaxValue)]
			public int Quantity { get; set; }
			[Range(0, double.MaxValue)]
			public decimal UnitPrice { get; set; }
			[Range(0, double.MaxValue)]
			public decimal DepositAmount { get; set; }
		}

		public class UpdateTimesRequest
		{
			[Required]
			public DateTime PickupAt { get; set; }
			[Required]
			public DateTime ReturnAt { get; set; }
		}

		public class SettlementRequest
		{
			[Range(0, int.MaxValue)]
			public int LateDays { get; set; }
			[Range(0, double.MaxValue)]
			public decimal RepairCost { get; set; }
			[Range(0, int.MaxValue)]
			public int DowntimeDays { get; set; }
			[Range(0, double.MaxValue)]
			public decimal MissingAccessoriesCost { get; set; }
			[Range(0, double.MaxValue)]
			public decimal CleaningCost { get; set; }
		}
	}
}
