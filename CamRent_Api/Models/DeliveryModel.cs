using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class DeliveryModel
	{
		public class CreateTaskRequest { [Required] public Guid BookingId { get; set; } public Guid? AssigneeUserId { get; set; } public string? TrackingCode { get; set; } public string? Notes { get; set; } [Range(0, double.MaxValue)] public decimal? DeliveryFee { get; set; } }
		public class UpdateStatusRequest { [Required] public DeliveryTaskStatus Status { get; set; } public DateTime? WhenUtc { get; set; } }
	}
}
