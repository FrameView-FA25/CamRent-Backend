using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Api.Models
{
	public class CameraModel
	{
		public class CameraRequest
		{
			public string Brand { get; set; } = string.Empty;
			public string Model { get; set; } = string.Empty;
			public string? Variant { get; set; }
			public string? SerialNumber { get; set; }
			public decimal EstimatedValueVnd { get; set; }
			public string? SpecsJson { get; set; }
			public List<IFormFile>? MediaFiles { get; set; } 
			
		}
	}
}
