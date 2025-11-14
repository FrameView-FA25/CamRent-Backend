using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace CamRent_Domain.Entities
{
    public class Accessory : BaseEntity
    {
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? Variant { get; set; }
        public string? SerialNumber { get; set; }

        public OwnershipType Ownership { get; set; }
        public Guid? OwnerUserId { get; set; }
        public User? OwnerUser { get; set; }

        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = default!;

        // Pricing base
        public decimal BaseDailyRate { get; set; }
        public decimal PlatformFeePercent { get; set; }

        // Deposit policy: percent of EstimatedValueVnd, with caps
        public decimal EstimatedValueVnd { get; set; }
        public decimal DepositPercent { get; set; }
        public decimal? DepositCapMinVnd { get; set; }
        public decimal? DepositCapMaxVnd { get; set; }

		[NotMapped]
		public ICollection<FileAsset> Media { get; set; } = new List<FileAsset>();
        public string? SpecsJson { get; set; }
        public ICollection<DeviceCategoryLink> Categories { get; set; } = new List<DeviceCategoryLink>();
    }
}
