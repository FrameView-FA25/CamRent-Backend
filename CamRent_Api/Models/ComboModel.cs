namespace CamRent_Api.Models
{
	public class ComboModel
	{
		public class CreateComboRequest 
		{ 
			public string Name { get; set; } = string.Empty; 
			public string? Description { get; set; } 
			public decimal? PriceOverride { get; set; } 
		}

		public class AddItemRequest 
		{ 
			public Guid? CameraId { get; set; } 
			public Guid? AccessoryId { get; set; } 
			public int Quantity { get; set; } 
		}
	}
}
