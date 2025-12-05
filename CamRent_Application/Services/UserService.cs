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
using static CamRent_Application.DTOs.AuthDTO;
using Microsoft.EntityFrameworkCore;

namespace CamRent_Application.Services
{
	public class UserService : IUserService
	{
		private readonly IUnitOfWork _unitOfWork;
		public UserService(IUnitOfWork uow)
		{
			_unitOfWork = uow;
		}

		public async Task<int> DeleteUser(Guid id)
		{
			await _unitOfWork.Repository<User>().DeleteAsync(id);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<List<User>> GetAllUsers()
		{
			var result = await _unitOfWork.Repository<User>()
				.ListAsync(include: q => q.Include(u => u.Roles));
			return result.ToList();
		}

		public async Task<User> GetUserProfileById(Guid id)
		{
			var users = await _unitOfWork.Repository<User>()
				.ListAsync(p => p.Id == id, include: q => q.Include(u => u.Roles));
			var user = users.FirstOrDefault() ?? throw new InvalidOperationException("User not found");
			return user;
		}

		public async Task<Guid> GetUserIdByManagerId(Guid managerId)
		{
			var user = await _unitOfWork.Repository<User>().ListAsync();
			return user!.FirstOrDefault(u => u.CreatedByUserId == managerId)?.Id ?? Guid.Empty;
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
