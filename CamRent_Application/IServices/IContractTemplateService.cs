using CamRent_Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IContractTemplateService
	{
		Task<byte[]> RenderBookingContractAsync(Contract contract);
		Task<byte[]> RenderVerificationContractAsync(Contract contract);
	}

}


