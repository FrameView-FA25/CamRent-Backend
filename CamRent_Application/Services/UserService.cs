using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using CamRent_Application.Interfaces;

namespace CamRent_Application.Services
{
	public class UserService : IUserService
	{
		private readonly IUnitOfWork _unitOfWork;
		public UserService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		public async Task<int> CreateUser(User user)
		{
			await _unitOfWork.Repository<User>().AddAsync(user);
			var result = await _unitOfWork.Complete();
			return result;
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

		public Task<User> GetUserByEmail(string email, string password)
		{
			throw new NotImplementedException();
		}

		public async Task<User> GetUserProfileById(Guid id)
		{
			var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
			return user;
		}

		public async Task<int> UpdateUser(User user)
		{
			await _unitOfWork.Repository<User>().UpdateAsync(user);
			var result = await _unitOfWork.Complete();
			return result;
		}

	}
}
