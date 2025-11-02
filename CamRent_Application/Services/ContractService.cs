using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class ContractService : IContractService
	{
		private readonly IUnitOfWork _unitOfWork;
		public ContractService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> CreateInstanceAsync(Guid bookingId, Guid templateId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var template = await _unitOfWork.Repository<ContractTemplate>().GetByIdAsync(templateId)
				?? throw new InvalidOperationException("Contract template not found");
			var instance = new ContractInstance
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				TemplateId = templateId,
				Status = ContractStatus.Sent,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<ContractInstance>().AddAsync(instance);
			await _unitOfWork.Complete();
			return instance.Id;
		}

		public async Task MarkSignedAsync(Guid contractInstanceId, string? signedFileUrl)
		{
			var instance = await _unitOfWork.Repository<ContractInstance>().GetByIdAsync(contractInstanceId)
				?? throw new InvalidOperationException("Contract instance not found");
			instance.Status = ContractStatus.Signed;
			instance.SignedFileUrl = signedFileUrl;
			await _unitOfWork.Repository<ContractInstance>().UpdateAsync(instance);
			await _unitOfWork.Complete();
		}
	}
}
