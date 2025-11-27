

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class AuthModel
	{
		public class LoginRequest
		{
			[Required, EmailAddress]
			[DefaultValue("user@gmail.com")]
			public string Email { get; set; } = default!;
			[Required]
			[DefaultValue("12345")]
			public string Password { get; set; } = default!;
		}

		public class ForgotPasswordRequest
		{
			[Required, EmailAddress]
			public string Email { get; set; } = default!;
			// Optional: UI link to redirect user for resetting
			[Url]
			public string? ContinueUrl { get; set; }
		}

		public class ResetPasswordRequest
		{
			[Required, EmailAddress]
			public string Email { get; set; } = default!;
			[Required]
			public string Token { get; set; } = default!;
			[Required, MinLength(6)]
			public string NewPassword { get; set; } = default!;
		}

	}
}
