using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class UserProfileModel
	{
		public class UpdateProfileRequest
		{
			// Thông tin tài khoản cơ bản
			[StringLength(256)]
			[EmailAddress]
			public string? Email { get; set; }

			[StringLength(128)]
			public string? FullName { get; set; }

			[StringLength(32)]
			public string? Phone { get; set; }

			// Địa chỉ (đơn giản hóa theo Address value object)
			[StringLength(128)]
			public string? Country { get; set; }

			[StringLength(128)]
			public string? Province { get; set; }

			[StringLength(128)]
			public string? District { get; set; }

			// KYC + thông tin ngân hàng
			[StringLength(64)]
			public string? NationalId { get; set; }

			[StringLength(32)]
			public string? KycStatus { get; set; }

			[StringLength(64)]
			public string? BankNo { get; set; }

			[StringLength(128)]
			public string? BankName { get; set; }

			[StringLength(128)]
			public string? BankAccName { get; set; }
		}
	}
}
