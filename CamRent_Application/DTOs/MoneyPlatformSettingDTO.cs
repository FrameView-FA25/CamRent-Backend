namespace CamRent_Application.DTOs
{
	public static class MoneyPlatformSettingDTO
	{
		public sealed class MoneyPlatformSettingResponse
		{
			public Guid Id { get; set; }
			public decimal UpfrontPercent { get; set; }
			public decimal PlatformFeePercent { get; set; }
			public decimal OwnerSharePercent { get; set; }
			public int LateFeeFirstNDays { get; set; }
			public decimal LateFeeFactorFirstN { get; set; }
			public decimal LateFeeFactorAfter { get; set; }
			public decimal DowntimeFactor { get; set; }
			public int CancelTimeMinutes { get; set; }
			public bool IsActive { get; set; }
			public DateTime CreatedAt { get; set; }
			public DateTime? UpdatedAt { get; set; }
		}

		public sealed class MoneyPlatformSettingRequest
		{
			public decimal UpfrontPercent { get; set; }
			public decimal PlatformFeePercent { get; set; }
			public decimal OwnerSharePercent { get; set; }
			public int LateFeeFirstNDays { get; set; }
			public decimal LateFeeFactorFirstN { get; set; }
			public decimal LateFeeFactorAfter { get; set; }
			public decimal DowntimeFactor { get; set; }
			public int CancelTimeMinutes { get; set; }
			public bool IsActive { get; set; } = true;
		}
	}
}

