using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class AuthDTO
	{
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

		public sealed class AuthResponse
		{
			public string Token { get; set; } = default!;
			public DateTime ExpiresAtUtc { get; set; }
			public string FullName { get; set; } = string.Empty;
			public string Email { get; set; } = string.Empty;
			public string[] Roles { get; set; } = Array.Empty<string>();

			// Thông tin bổ sung cho FE sau khi đăng nhập
			public DateTime CreatedAt { get; set; }
			public string PhoneNumber { get; set; } = string.Empty;
			public Address? Address { get; set; }
		}

		public sealed class JwtOptions
		{
			public string Key { get; set; } = default!;
			public string Issuer { get; set; } = default!;
			public string Audience { get; set; } = default!;
			public int ExpireMinutes { get; set; } = 120;
		}
	}
}
