using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class UserProfileModel
	{
		public class UpdateProfileRequest 
		{ 
			[StringLength(64)] 
			public string? BankNo { get; set; } 
			[StringLength(128)] 
			public string? BankName { get; set; } 
			[StringLength(128)] 
			public string? BankAccName { get; set; } 
		}
		public class UpdateSignRequest
		{
			public string SignatureBase64 { get; set; } = default!;
		}
	}
}
