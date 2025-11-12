using System.Security.Cryptography;
using System.Text;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CamRent_Application.Services
{
	public sealed class PasswordResetService : IPasswordResetService
	{
		private readonly IUnitOfWork _uow;
		private readonly IEmailService _email;
		private readonly IPasswordHasher<User> _hasher;

		public PasswordResetService(IUnitOfWork uow, IEmailService email, IPasswordHasher<User> hasher)
		{
			_uow = uow;
			_email = email;
			_hasher = hasher;
		}

		public async Task RequestResetAsync(string email, string? continueUrl = null, CancellationToken ct = default)
		{
			var user = (await _uow.Repository<User>().ListAsync(u => u.Email == email.Trim())).FirstOrDefault();
			if (user == null) return; // don't leak existence

			// Invalidate old tokens
			var oldTokens = await _uow.Repository<ResetPasswordToken>().ListAsync(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);
			foreach (var t in oldTokens)
			{
				t.IsUsed = true;
				t.UsedAt = DateTime.UtcNow;
				await _uow.Repository<ResetPasswordToken>().UpdateAsync(t);
			}

			// Create new token
			var token = GenerateToken();
			var entity = new ResetPasswordToken
			{
				Id = Guid.NewGuid(),
				UserId = user.Id,
				Token = token,
				ExpiresAt = DateTime.UtcNow.AddMinutes(30),
				IsUsed = false,
				CreatedAt = DateTime.UtcNow
			};
			await _uow.Repository<ResetPasswordToken>().AddAsync(entity);
			await _uow.Complete();

			var link = AppendQuery(continueUrl ?? "", new Dictionary<string, string>
			{
				{ "email", user.Email },
				{ "token", token }
			});
			var html = $"<p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản {user.Email}.</p><p>Nhấp vào liên kết sau để đặt lại (hết hạn sau 30 phút):</p><p><a href=\"{link}\">{link}</a></p>";
			await _email.SendAsync(user.Email, "CamRent - Đặt lại mật khẩu", html, ct);
		}

		public async Task<bool> ResetAsync(string email, string token, string newPassword, CancellationToken ct = default)
		{
			var user = (await _uow.Repository<User>().ListAsync(u => u.Email == email.Trim())).FirstOrDefault();
			if (user == null) return false;

			var t = (await _uow.Repository<ResetPasswordToken>()
				.ListAsync(x => x.UserId == user.Id && x.Token == token))
				.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
			if (t == null) return false;
			if (t.IsUsed || t.ExpiresAt < DateTime.UtcNow) return false;

			user.PasswordHash = _hasher.HashPassword(user, newPassword);
			await _uow.Repository<User>().UpdateAsync(user);

			t.IsUsed = true;
			t.UsedAt = DateTime.UtcNow;
			await _uow.Repository<ResetPasswordToken>().UpdateAsync(t);

			await _uow.Complete();
			return true;
		}

		private static string GenerateToken()
		{
			Span<byte> buf = stackalloc byte[32];
			RandomNumberGenerator.Fill(buf);
			return Convert.ToBase64String(buf).Replace("+", "-").Replace("/", "_").TrimEnd('=');
		}

		private static string AppendQuery(string url, IDictionary<string, string> kv)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				// fallback to API deep-link if no continueUrl provided
				return $"https://example.com/reset-password?email={Uri.EscapeDataString(kv["email"])}&token={Uri.EscapeDataString(kv["token"])}";
			}
			var hasQuery = url.Contains("?");
			var prefix = hasQuery ? "&" : "?";
			var query = string.Join("&", kv.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
			return url + prefix + query;
		}
	}
}

