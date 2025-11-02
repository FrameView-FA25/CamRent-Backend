using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class UserProfileModel
	{
		public class UpdateProfileRequest { [StringLength(64)] public string? NationalId { get; set; } [StringLength(32)] public string? KycStatus { get; set; } [StringLength(64)] public string? BankNo { get; set; } [StringLength(128)] public string? BankName { get; set; } [StringLength(128)] public string? BankAccName { get; set; } }
	}
}
