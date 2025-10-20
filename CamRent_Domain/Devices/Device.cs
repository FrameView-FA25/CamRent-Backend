using CamRent_Domain.Branches;
using CamRent_Domain.Common;
using CamRent_Domain.Files;
using CamRent_Domain.Users;

namespace CamRent_Domain.Devices
{
    public class Device : BaseEntity
    {
        public DeviceCategory Category { get; set; }
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
    }
}

