using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	public class Contract : BaseEntity
	{
		public Guid Id { get; set; }

		public ContractType Type { get; set; }

		public Guid? BookingId { get; set; }
		public Booking? Booking { get; set; }

		public Guid? VerificationId { get; set; }   // cho Verification
		public VerificationRequest? Verification { get; set; }

		public Guid? BranchId { get; set; }
		public Branch? Branch { get; set; }

		public ContractStatus Status { get; set; }

		// File PDF cuối cùng
		public Guid? FileAssetId { get; set; }
		public FileAsset? FileAsset { get; set; }

		public string? FileHash { get; set; }     // SHA256

		public DateTime CreatedAt { get; set; }
		public DateTime? SignedAt { get; set; }

		public ICollection<ContractSignature> Signatures { get; set; } = new List<ContractSignature>();
	}

	public class ContractSignature : BaseEntity
	{
		public Guid Id { get; set; }

		public Guid ContractId { get; set; }
		public Contract Contract { get; set; } = default!;

		public ContractSignerRole Role { get; set; }

		public Guid? UserId { get; set; }
		public User? User { get; set; }

		public bool IsSigned { get; set; }
		public DateTime? SignedAt { get; set; }

		// Cloudinary file
		public Guid? SignatureAssetId { get; set; }
		public FileAsset? SignatureAsset { get; set; }

		public string? SignedIp { get; set; }
		public string? SignedUserAgent { get; set; }

		public string? DocumentHashAtSignTime { get; set; }
	}

}

