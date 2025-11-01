using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class UserProfileService : IUserProfileService
	{
		private readonly IUnitOfWork _unitOfWork;
		public UserProfileService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<(Guid id, Guid userId, string? nationalId, string kycStatus, string? bankNo, string? bankName, string? bankAccName)> GetByUserIdAsync(Guid userId)
		{
			var profile = (await _unitOfWork.Repository<UserProfile>().ListAsync(p => p.UserId == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User profile not found");
			return (profile.Id, profile.UserId, profile.NationalIdNumber, profile.KycStatus, profile.BankAccountNumber, profile.BankName, profile.BankAccountName);
		}

		public async Task UpdateAsync(Guid userId, string? nationalId, string? kycStatus, string? bankNo, string? bankName, string? bankAccName)
		{
			var profile = (await _unitOfWork.Repository<UserProfile>().ListAsync(p => p.UserId == userId)).FirstOrDefault();
			if (profile == null)
			{
				profile = new UserProfile { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow };
				await _unitOfWork.Repository<UserProfile>().AddAsync(profile);
			}
			profile.NationalIdNumber = nationalId ?? profile.NationalIdNumber;
			if (!string.IsNullOrWhiteSpace(kycStatus)) profile.KycStatus = kycStatus!;
			profile.BankAccountNumber = bankNo ?? profile.BankAccountNumber;
			profile.BankName = bankName ?? profile.BankName;
			profile.BankAccountName = bankAccName ?? profile.BankAccountName;
			await _unitOfWork.Repository<UserProfile>().UpdateAsync(profile);
			await _unitOfWork.Complete();
		}
	}
}
