

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

	}
}
