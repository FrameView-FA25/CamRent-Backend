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

		Task<(Guid id, Guid userId, string? nationalId, string kycStatus, string? bankNo, string? bankName, string? bankAccName)> GetProfileAsync(Guid userId);

		/// <summary>
		/// Cập nhật hồ sơ của user: thông tin cơ bản (email, tên, phone, address) + KYC + ngân hàng.
		/// </summary>
		Task UpdateProfileAsync(
		 Guid userId,
		 string? nationalId,
		 string? kycStatus,
		 string? bankNo,
		 string? bankName,
		 string? bankAccName,
		 string? fullName,
		 string? phone,
		 string? email,
		 string? country,
		 string? province,
		 string? district);
	}
}
