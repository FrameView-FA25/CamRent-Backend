using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.AuthDTO;

namespace CamRent_Application.Services
{
		public class AuthService : IAuthService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IPasswordHasher<User> _hasher;
		private readonly JwtOptions _jwt;

		public AuthService(IUnitOfWork uow,
						   IPasswordHasher<User> hasher,
						   IOptions<JwtOptions> jwt)
		{
			_unitOfWork = uow;
			_hasher = hasher;
			_jwt = jwt.Value;
		}

		public async Task<Guid> Register(RegisterRequest request, Guid? userId, UserRole role)
		{
			var email = request.Email.Trim();

			var userExists = await _unitOfWork.Repository<User>()
				.ListAsync(u => u.Email == email);
			if (userExists.Any())
				return Guid.Empty;

			var user = new User
			{
				Id = Guid.NewGuid(),
				Email = email,
				NormalizedEmail = email.ToUpper(),
				Phone = request.Phone?.Trim() ?? string.Empty,
				FullName = request.FullName?.Trim() ?? string.Empty,
				Status = UserStatus.Active,
				CreatedAt = DateTime.UtcNow,
			};
			if(userId != Guid.Empty)
			{
				user.CreatedByUserId = userId;
			}
			else
			{
				user.CreatedByUserId = user.Id;
			}
			user.PasswordHash = _hasher.HashPassword(user, request.Password);
			await _unitOfWork.Repository<User>().AddAsync(user);

			await _unitOfWork.Repository<UserRoleMapping>().AddAsync(new UserRoleMapping
			{
				User = user,
				Role = role,
				CreatedAt = DateTime.UtcNow
			});

			await _unitOfWork.Complete();
			return user.Id;
		}

		public async Task<AuthResponse> GetToken(string email, string password)
		{
			var identifier = email.Trim();

			// Cho phép đăng nhập bằng email hoặc số điện thoại
			var users = await _unitOfWork.Repository<User>()
				.ListAsync(u => identifier.Contains("@")
					? u.Email == identifier
					: u.Phone == identifier);

			var user = users.FirstOrDefault();
			if (user is null)
				throw new Exception("Sai thông tin đăng nhập.");

			user.Roles = (await _unitOfWork.Repository<UserRoleMapping>()
				.ListAsync(r => r.UserId == user.Id)).ToList();

			var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
			if (result == PasswordVerificationResult.Failed)
				throw new Exception("Sai thông tin đăng nhập.");

			if (user.Status != UserStatus.Active)
				throw new Exception("Tài khoản chưa hoạt động hoặc bị khóa.");

			return GenerateJwt(user);
		}

		public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
		{
			var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
			if (user == null) return false;

			// Verify current password
			var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
			if (verify == PasswordVerificationResult.Failed) return false;

			// Update to new password
			user.PasswordHash = _hasher.HashPassword(user, newPassword);
			await _unitOfWork.Repository<User>().UpdateAsync(user);
			await _unitOfWork.Complete();
			return true;
		}

		private AuthResponse GenerateJwt(User user)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new List<Claim>
			{
				new Claim("sub", user.Id.ToString()),
				new Claim("jti", Guid.NewGuid().ToString()),
				new Claim("email", user.Email),
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
				new Claim("uid", user.Id.ToString())
			};

			foreach (var r in user.Roles.Select(x => x.Role.ToString()))
				claims.Add(new Claim(ClaimTypes.Role, r));

			var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes);

			var token = new JwtSecurityToken(
				issuer: _jwt.Issuer,
				audience: _jwt.Audience,
				claims: claims,
				notBefore: DateTime.UtcNow,
				expires: expires,
				signingCredentials: creds
			);

			var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

			return new AuthResponse
			{
				Token = tokenStr
			};
		}
	}
}
