using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IVerificationService
	{
		Task<Guid> CreateRequestAsync(Guid? targetUserId, Guid? branchId, string? notes);
	}
}
