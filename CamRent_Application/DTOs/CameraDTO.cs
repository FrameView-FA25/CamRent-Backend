using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class CameraDTO
	{
		public class CameraResponseDTO
		{
			public Guid Id { get; set; }
			public string Brand { get; set; } = string.Empty;
			public string Model { get; set; } = string.Empty;
			public string? Variant { get; set; }
			public string? SerialNumber { get; set; }
			public string BranchName { get; set; }

			public string BranchAddress { get; set; }

			public ItemType ItemType => ItemType.Camera;
			// Pricing base
			public decimal BaseDailyRate { get; set; }

			// Deposit policy: percent of EstimatedValueVnd, with caps
			public decimal EstimatedValueVnd { get; set; }
			public decimal DepositPercent { get; set; }
			public decimal? DepositCapMinVnd { get; set; }
			public decimal? DepositCapMaxVnd { get; set; }
			public string? SpecsJson { get; set; }

			public bool IsConfirmed { get; set; } = false;
			public bool IsAvailable { get; set; } = false;
			public AssetLocation Location { get; set; } = AssetLocation.WithOwner;
			public Guid? OwnerUserId { get; set; }
			public string? OwnerName { get; set; }
			public List<FileAssetDTO> Media { get; set; } = new();
		}
	}
}
