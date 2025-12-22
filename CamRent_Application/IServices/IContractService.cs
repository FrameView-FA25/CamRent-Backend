using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.ContractDTO;

namespace CamRent_Application.IServices
{
	public interface IContractService
	{
		Task<Contract> CreateBookingContractAsync(Guid bookingId, Guid staffUserId);
		Task<Contract> CreateVerificationContractAsync(Guid verificationId, Guid staffUserId);
		Task<Contract> SignContractAsync(Guid contractId, ContractSignerRole role,
			string signatureBase64, Guid? userId, string? ip, string? userAgent);
		Task<byte[]?> DownloadContractPdfAsync(Guid contractId);
		Task GenerateAndUploadFinalPdfAsync(Guid contractId, List<ContractSignature> signatures);
		Task<Contract?> GetByIdAsync(Guid contractId);
		Task DeleteBookingContractAsync(Guid bookingId);

		Task<ContractResponse?> GetContractByIdAsync(Guid contractId);
		Task<List<ContractResponse>> GetContractsAsync();
	}

}
