using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class VerificationService : IVerificationService
	{
		private readonly IUnitOfWork _unitOfWork;
		public VerificationService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> CreateRequestAsync(Guid? targetUserId, Guid? branchId, string? notes)
		{
			var request = new VerificationRequest
			{
				Id = Guid.NewGuid(),
				Type = targetUserId.HasValue ? "user_kyc" : "device_verification",
				Status = "pending",
				TargetUserId = targetUserId,
				BranchId = branchId,
				Notes = notes,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<VerificationRequest>().AddAsync(request);
			await _unitOfWork.Complete();
			return request.Id;
		}
	}
}
