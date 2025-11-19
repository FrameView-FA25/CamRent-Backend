using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class ComboModel
	{
		public class CreateComboRequest 
		{ 
			[Required, MinLength(2)]
			public string Name { get; set; } = string.Empty; 
			public string? Description { get; set; } 
			[Range(0, double.MaxValue)]
			public decimal? PriceOverride { get; set; } 
		}

		public class AddItemRequest 
		{ 
			public Guid? CameraId { get; set; } 
			public Guid? AccessoryId { get; set; } 
		}
	}
}
