using System;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.VerificationRequestDTO;

namespace CamRent_Application.IServices
{
	public interface IVerificationService
	{
		Task<List<VerificationResponseDTO>> GetVerifications();
		Task<List<VerificationResponseDTO>> GetVerificationByStaffId(Guid id);
		Task<List<VerificationResponseDTO>> GetVerificationByOwnerId(Guid id);
		Task<List<VerificationResponseDTO>> GetVerificationByManagerId(Guid id);
		Task<int> AssignStaffToVerification(Guid staffId, Guid verificationRequest);
		Task<int> CreateVerificationAsync(CreateVerificationRequestDTO verificationRequestDTO, Guid ownerId);
	}
}
