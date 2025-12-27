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

		// Cập nhật thông tin tài khoản của chính user (email, tên, địa chỉ dạng chuỗi, phone)
		public class UpdateAccountRequest
		{
			[EmailAddress]
			[StringLength(256)]
			public string? Email { get; set; }

			[StringLength(128)]
			public string? FullName { get; set; }

			[StringLength(32)]
			public string? Phone { get; set; }

			// Địa chỉ dạng chuỗi đơn giản (VD: "123 Lê Lợi, Quận 1, TP.HCM")
			[StringLength(256)]
			public string? Address { get; set; }
		}
		public class UpdateSignRequest
		{
			public string SignatureBase64 { get; set; } = default!;
		}
	}
}
