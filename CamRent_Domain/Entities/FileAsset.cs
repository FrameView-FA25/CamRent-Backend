using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class FileAsset : BaseEntity
    {
        public string Url { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long? SizeBytes { get; set; }
        public string? Label { get; set; }
        public string? Provider { get; set; }
        public string? ProviderKey { get; set; }
		// 🔹 Ai là chủ file (Camera / Accessory / User / ...)
		public Guid? OwnerId { get; set; }

		// 🔹 Loại owner
		public FileOwnerType? OwnerType { get; set; }
	}
}

