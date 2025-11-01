using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IContractService
	{
		Task<Guid> CreateInstanceAsync(Guid bookingId, Guid templateId);
		Task MarkSignedAsync(Guid contractInstanceId, string? signedFileUrl);
	}
}
