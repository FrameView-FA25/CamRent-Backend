using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;     
using System.Security.Claims;             
using Microsoft.IdentityModel.Tokens;		
using System.Text;
using static CamRent_Application.DTOs.UserDTO;

namespace CamRent_Application.Services
{
	public class UserService : IUserService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IPasswordHasher<User> _hasher;
		private readonly JwtOptions _jwt;

		public UserService(IUnitOfWork uow,
						   IPasswordHasher<User> hasher,
						   IOptions<JwtOptions> jwt)
		{
			_unitOfWork = uow;
			_hasher = hasher;
			_jwt = jwt.Value;
		}

		public async Task<int> DeleteUser(Guid id)
		{
			await _unitOfWork.Repository<User>().DeleteAsync(id);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<List<User>> GetAllUsers()
		{
			var result = await _unitOfWork.Repository<User>().GetAllAsync();
			return (List<User>)result;
		}

		public async Task<User> GetUserProfileById(Guid id)
		{
			var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
			return user!;
		}

		public async Task<string> Register(RegisterRequest request)
		{
			var email = request.Email.Trim();

			var userExists = await _unitOfWork.Repository<User>()
				.ListAsync(u => u.Email == email);
			if (userExists.Any())
				return "Email đã tồn tại.";

			var user = new User
			{
				Id = Guid.NewGuid(),
				Email = email,
				NormalizedEmail = email.ToUpper(),
				Phone = request.Phone?.Trim() ?? string.Empty,
				FullName = request.FullName?.Trim() ?? string.Empty,
				Status = UserStatus.Active,
				CreatedAt = DateTime.UtcNow
			};

			user.PasswordHash = _hasher.HashPassword(user, request.Password);
			await _unitOfWork.Repository<User>().AddAsync(user);

			if (request.Role == UserRole.Renter || request.Role == UserRole.Owner)
			{
				await _unitOfWork.Repository<UserRoleMapping>().AddAsync(new UserRoleMapping
				{
					User = user,
					Role = request.Role
				});
			}

			await _unitOfWork.Complete();
			return "Đăng ký thành công.";
		}

		public async Task<AuthResponse> GetToken(string email, string password)
		{
			var user = (await _unitOfWork.Repository<User>()
				.ListAsync(u => u.Email == email.Trim()))
				.FirstOrDefault();

			if (user is null)
				throw new Exception("Sai email hoặc mật khẩu.");

			var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
			if (result == PasswordVerificationResult.Failed)
				throw new Exception("Sai email hoặc mật khẩu.");

			if (user.Status != UserStatus.Active)
				throw new Exception("Tài khoản chưa hoạt động hoặc bị khóa.");

			return GenerateJwt(user);
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
				Token = tokenStr,
				ExpiresAtUtc = expires,
				FullName = user.FullName,
				Roles = user.Roles.Select(x => x.Role.ToString()).ToArray(),
				Email = user.Email
			};
		}

		public async Task<int> UpdateUser(User user)
		{
			await _unitOfWork.Repository<User>().UpdateAsync(user);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<(Guid id, Guid userId, string? nationalId, string kycStatus, string? bankNo, string? bankName, string? bankAccName)> GetProfileAsync(Guid userId)
		{
			var user = (await _unitOfWork.Repository<User>().ListAsync(p => p.Id == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");
			return (user.Id, user.Id, user.NationalIdNumber, user.KycStatus, user.BankAccountNumber, user.BankName, user.BankAccountName);
		}

		public async Task UpdateProfileAsync(Guid userId, string? nationalId, string? kycStatus, string? bankNo, string? bankName, string? bankAccName)
		{
			var user = (await _unitOfWork.Repository<User>().ListAsync(p => p.Id == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");
			user.NationalIdNumber = nationalId ?? user.NationalIdNumber;
			if (!string.IsNullOrWhiteSpace(kycStatus)) user.KycStatus = kycStatus!;
			user.BankAccountNumber = bankNo ?? user.BankAccountNumber;
			user.BankName = bankName ?? user.BankName;
			user.BankAccountName = bankAccName ?? user.BankAccountName;
			await _unitOfWork.Repository<User>().UpdateAsync(user);
			await _unitOfWork.Complete();
		}
	}
}
