

using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class AuthModel
	{
		public class LoginRequest
		{
			[Required, EmailAddress]
			public string Email { get; set; } = default!;
			[Required]
			public string Password { get; set; } = default!;
		}

		public class RegisterRequest
		{
			[Required, EmailAddress]
			public string Email { get; set; } = default!;
			public string Phone { get; set; } = string.Empty;
			[Required, MinLength(6)]
			public string Password { get; set; } = default!;
			[Required]
			public string FullName { get; set; } = default!;
		}

	}
}
