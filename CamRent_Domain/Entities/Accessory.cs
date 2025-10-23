using CamRent_Domain.Common;

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

        public decimal BaseDailyRate { get; set; }
        public decimal DepositPercent { get; set; }
        public decimal PlatformFeePercent { get; set; }

        public ICollection<FileAsset> Media { get; set; } = new List<FileAsset>();
        public string? SpecsJson { get; set; }
        public ICollection<DeviceCategoryLink> Categories { get; set; } = new List<DeviceCategoryLink>();
    }
}
