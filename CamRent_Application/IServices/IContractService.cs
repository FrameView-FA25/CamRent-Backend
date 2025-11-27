using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IContractService
	{
		Task<Contract> CreateBookingContractAsync(Guid bookingId, Guid staffUserId);
		Task<Contract> SignContractAsync(Guid contractId, ContractSignerRole role,
			string signatureBase64, Guid? userId, string? ip, string? userAgent);
		Task<byte[]?> DownloadContractPdfAsync(Guid contractId);
	}

}
