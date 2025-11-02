using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class VerificationModel
	{
		public class CreateVerificationRequest { public Guid? TargetUserId { get; set; } public Guid? BranchId { get; set; } [StringLength(512)] public string? Notes { get; set; } }
	}
}
