using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System.Text.Json;

namespace CamRent_Application.Services
{
	public class ContractService : IContractService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IContractTemplateService _templateService;
		private readonly IFileStorageService _fileStorage;

		public ContractService(IUnitOfWork unitOfWork, IContractTemplateService templateService, IFileStorageService fileStorage)
		{
			_unitOfWork = unitOfWork;
			_templateService = templateService;
			_fileStorage = fileStorage;
		}

		public async Task<Guid> CreateInstanceAsync(Guid bookingId, Guid templateId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var template = await _unitOfWork.Repository<ContractTemplate>().GetByIdAsync(templateId)
				?? throw new InvalidOperationException("Contract template not found");
			var instance = new Contract
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				TemplateId = templateId,
				Status = ContractStatus.Sent,
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<Contract>().AddAsync(instance);
			await _unitOfWork.Complete();
			return instance.Id;
		}

		public async Task MarkSignedAsync(Guid contractInstanceId, string? signedFileUrl)
		{
			var instance = await _unitOfWork.Repository<Contract>().GetByIdAsync(contractInstanceId)
				?? throw new InvalidOperationException("Contract instance not found");
			instance.Status = ContractStatus.Signed;
			instance.SignedFileUrl = signedFileUrl;
			await _unitOfWork.Repository<Contract>().UpdateAsync(instance);
			await _unitOfWork.Complete();
		}

		public async Task<Guid> GenerateAndStoreContractAsync(Guid bookingId, CancellationToken cancellationToken = default)
		{
			// Lấy booking để đảm bảo tồn tại
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");

			// Chọn template mới nhất làm default
			var templates = await _unitOfWork.Repository<ContractTemplate>().ListAsync();
			var template = templates
				.OrderByDescending(t => t.CreatedAt)
				.FirstOrDefault() ?? throw new InvalidOperationException("No contract template configured");

			// Tìm contract đã tồn tại cho booking (nếu có thì dùng lại)
			var existingContracts = await _unitOfWork.Repository<Contract>().ListAsync(c => c.BookingId == bookingId);
			var contract = existingContracts
				.OrderByDescending(c => c.CreatedAt)
				.FirstOrDefault();

			if (contract == null)
			{
				contract = new Contract
				{
					Id = Guid.NewGuid(),
					BookingId = bookingId,
					TemplateId = template.Id,
					Status = ContractStatus.Draft,
					CreatedAt = DateTime.UtcNow
				};
				await _unitOfWork.Repository<Contract>().AddAsync(contract);
				await _unitOfWork.Complete();
			}

			// Sinh PDF hợp đồng chính thức
			var pdfBytes = await _templateService.GenerateContractPdfAsync(contract.Id, cancellationToken);

			// Upload lên storage, gắn với contract
			var file = await _fileStorage.UploadAsync(
				pdfBytes,
				fileName: $"contract_{contract.Id}.pdf",
				contentType: "application/pdf",
				ownerId: contract.Id,
				ownerType: FileOwnerType.ContractDocument,
				folder: $"camrent/contracts/{contract.Id}",
				label: "rental-contract");

			contract.SignedFileUrl = file.Url;
			contract.Status = ContractStatus.Signed;
			await _unitOfWork.Repository<Contract>().UpdateAsync(contract);

			// Ghi lại event để tăng tính pháp lý (audit trail)
			var ev = new ContractEvent
			{
				Id = Guid.NewGuid(),
				ContractId = contract.Id,
				Type = "auto_generated",
				DataJson = JsonSerializer.Serialize(new
				{
					bookingId,
					fileId = file.Id,
					fileUrl = file.Url
				}),
				OccurredAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<ContractEvent>().AddAsync(ev);

			await _unitOfWork.Complete();
			return contract.Id;
		}
	}
}
