using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Api.Models
{
	public class AccessoryModel
	{
		public class AccessoryRequest
		{
			public string Brand { get; set; } = string.Empty;
			public string Model { get; set; } = string.Empty;
			public string? Variant { get; set; }
			public string? SerialNumber { get; set; }
			public decimal EstimatedValueVnd { get; set; }
			public string? SpecsJson { get; set; }
			public ICollection<FileAsset> Media { get; set; } = new List<FileAsset>();
			public ICollection<DeviceCategoryLink> Categories { get; set; } = new List<DeviceCategoryLink>();
		}

	}
}
