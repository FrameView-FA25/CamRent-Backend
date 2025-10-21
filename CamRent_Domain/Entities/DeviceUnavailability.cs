using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
    public class DeviceUnavailability : BaseEntity
    {
        public Guid DeviceId { get; set; }
        public Device Device { get; set; } = default!;

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Reason { get; set; } = string.Empty; // maintenance, internal_use, etc.
    }
}

