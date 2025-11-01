using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IUserProfileService
	{
		Task<(Guid id, Guid userId, string? nationalId, string kycStatus, string? bankNo, string? bankName, string? bankAccName)> GetByUserIdAsync(Guid userId);
		Task UpdateAsync(Guid userId, string? nationalId, string? kycStatus, string? bankNo, string? bankName, string? bankAccName);
	}
}
