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
		Task UpdateUserSignAsync(Guid userId, string signatureBase64);
	}	
}
