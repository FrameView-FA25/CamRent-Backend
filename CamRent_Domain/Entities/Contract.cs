using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class ContractTemplate : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "v1";
        public string TemplateUrl { get; set; } = string.Empty;
		public ContractType Type { get; set; }
	}

	public class Contract : BaseEntity
	{
		public Guid? BookingId { get; set; }
		public Booking? Booking { get; set; }

		public Guid? VerificationId { get; set; }
		public VerificationRequest? Verification { get; set; }

		public Guid TemplateId { get; set; }
		public ContractTemplate Template { get; set; } 

		public ContractStatus Status { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public string SignedFileUrl { get; set; } = string.Empty;
		public ICollection<ContractSigner> Signers { get; set; } = new List<ContractSigner>();
		public ICollection<ContractEvent> Events { get; set; } = new List<ContractEvent>();
	}

	public class ContractSigner : BaseEntity
	{
		public Guid ContractId { get; set; }
		public Contract Contract { get; set; } 

		public string Role { get; set; } = string.Empty; // renter, owner, platform
		public string Email { get; set; } = string.Empty;
		public string FullName { get; set; } = string.Empty;
		public int SignOrder { get; set; } = 1;
		public string Status { get; set; } = "pending"; // pending, signed
		public DateTime? SignedAt { get; set; }
	}

	public class ContractEvent : BaseEntity
	{
		public Guid ContractId { get; set; }
		public Contract Contract { get; set; } 
		public string Type { get; set; } = string.Empty; // provider_webhook, status_change, error
		public string DataJson { get; set; } = string.Empty;
		public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
	}

}

