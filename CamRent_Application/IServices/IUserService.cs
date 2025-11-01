using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IUserService
	{
		Task<User> GetUserByEmail(string email, string password);
		Task<List<User>> GetAllUsers();
		Task<User> GetUserProfileById(Guid id);
		Task<int> CreateUser(User user);
		Task<int> UpdateUser(User user);
		Task<int> DeleteUser(Guid id);
	}
}
