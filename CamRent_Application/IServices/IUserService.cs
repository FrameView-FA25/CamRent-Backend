using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.UserDTO;

namespace CamRent_Application.IServices
{
	public interface IUserService
	{
		Task<AuthResponse> GetToken(string email, string password);
		Task<List<User>> GetAllUsers();
		Task<User> GetUserProfileById(Guid id);
		Task<String> Register(RegisterRequest user);
		Task<int> UpdateUser(User user);
		Task<int> DeleteUser(Guid id);
	}
}
