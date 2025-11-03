using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Api.Models
{
	public class CameraModel
	{
		public class CreateCameraRequest
		{
			public string Brand { get; set; } = string.Empty;
			public string Model { get; set; } = string.Empty;
			public string? Variant { get; set; }
			public string? SerialNumber { get; set; }
			public OwnershipType Ownership { get; set; }
			public Guid? OwnerUserId { get; set; }
			public Guid BranchId { get; set; }

			// Pricing base
			public decimal BaseDailyRate { get; set; }
			public decimal PlatformFeePercent { get; set; }

			// Deposit policy: percent of EstimatedValueVnd, with caps
			public decimal EstimatedValueVnd { get; set; }
			public decimal DepositPercent { get; set; }
			public decimal? DepositCapMinVnd { get; set; }
			public decimal? DepositCapMaxVnd { get; set; }

			public ICollection<FileAsset> Media { get; set; } = new List<FileAsset>();
			public string? SpecsJson { get; set; }
		}
	}
}
