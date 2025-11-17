using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class BookingModel
	{

		public class AddToCartRequest
		{
			public Guid Id { get; set; }
			public ItemType Type { get; set; }
			public int Quantity { get; set; } = 1;
		}

		public class RemoveFromCartRequest
		{
			public Guid Id { get; set; }
			public ItemType Type { get; set; }
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
