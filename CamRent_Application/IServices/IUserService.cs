using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.AuthDTO;

namespace CamRent_Application.IServices
{
	public interface IUserService
	{
		
		Task<List<User>> GetAllUsers();
		Task<User> GetUserProfileById(Guid id);
		Task<Guid> GetUserIdByManagerId(Guid managerId);
		Task<int> UpdateUser(User user);
		Task<int> DeleteUser(Guid id);

		Task<(Guid id, string? bankNo, string? bankName, string? bankAccName)> GetProfileAsync(Guid userId);
		Task UpdateProfileAsync(Guid userId, string? bankNo, string? bankName, string? bankAccName);
		Task<int> UpdateUserSignAsync(Guid userId, string signatureBase64);
			// Cập nhật thông tin tài khoản cơ bản cho chính user (email, tên, phone, địa chỉ)
			Task UpdateAccountAsync(Guid userId, string? email, string? fullName, string? phone, string? country, string? province, string? district);
		Task<(Guid assetId, string url)> UpdateAvatarAsync(Guid userId, Microsoft.AspNetCore.Http.IFormFile avatarFile, CancellationToken ct = default);
	}	
}
