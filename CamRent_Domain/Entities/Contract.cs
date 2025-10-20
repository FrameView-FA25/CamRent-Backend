using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class ContractTemplate : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "v1";
        public string TemplateUrl { get; set; } = string.Empty;
    }

    public class ContractInstance : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public Guid TemplateId { get; set; }
        public ContractTemplate Template { get; set; } = default!;

        public ContractStatus Status { get; set; }
        public string? SignedFileUrl { get; set; }
        public string? Provider { get; set; }
        public string? ProviderEnvelopeId { get; set; }
    }
}

