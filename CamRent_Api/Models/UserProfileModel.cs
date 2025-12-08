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

		// Cập nhật thông tin tài khoản của chính user (email, tên, địa chỉ, phone)
		public class UpdateAccountRequest
		{
			[EmailAddress]
			[StringLength(256)]
			public string? Email { get; set; }

			[StringLength(128)]
			public string? FullName { get; set; }

			[StringLength(32)]
			public string? Phone { get; set; }

			// Địa chỉ đơn giản hoá theo 3 field trong Address value object
			[StringLength(128)]
			public string? Country { get; set; }

			[StringLength(128)]
			public string? Province { get; set; }

			[StringLength(128)]
			public string? District { get; set; }
		}
		public class UpdateSignRequest
		{
			public string SignatureBase64 { get; set; } = default!;
		}
	}
}
