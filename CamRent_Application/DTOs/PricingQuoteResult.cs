namespace CamRent_Application.DTOs
{
	public class PricingQuoteResult
	{
		public Guid BookingId { get; set; }
		public int Days { get; set; }
		public decimal RentalTotal { get; set; }
		public decimal DepositTotal { get; set; }
		public decimal PlatformFee { get; set; }
		public decimal NetRevenue { get; set; }
		public decimal OwnerShare { get; set; }
		public decimal PlatformNet { get; set; }
	}

	public class DepositSettlement
	{
		public Guid BookingId { get; set; }
		public int LateDays { get; set; }
		public int DowntimeDays { get; set; }
		public decimal LateFee { get; set; }
		public decimal RepairCost { get; set; }
		public decimal DowntimeFee { get; set; }
		public decimal MissingAccessoriesCost { get; set; }
		public decimal CleaningCost { get; set; }
		public decimal TotalDeductions { get; set; }
		public decimal DepositCollected { get; set; }
		public decimal DepositRefund { get; set; }
		public decimal ExtraDueFromCustomer { get; set; }
	}
}
