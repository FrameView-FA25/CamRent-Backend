using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using PayOS.Exceptions;
using System.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CamRent_Application.Services
{
	public class ContractService : IContractService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IContractTemplateService _templateService;
		private readonly IFileStorageService _fileStorage;

		public ContractService(
			IUnitOfWork unitOfWork,
			IContractTemplateService templateService,
			IFileStorageService fileStorage)
		{
			_unitOfWork = unitOfWork;
			_templateService = templateService;
			_fileStorage = fileStorage;
		}

		#region Public APIs

		/// <summary>
		/// Tạo hợp đồng cho booking: pending signatures (Renter + Platform)
		/// </summary>
		public async Task<Contract> CreateBookingContractAsync(Guid bookingId, Guid staffUserId)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();
			var contractRepo = _unitOfWork.Repository<Contract>();
			var signatureRepo = _unitOfWork.Repository<ContractSignature>();

			var booking = await bookingRepo.GetByIdAsync(bookingId)
				?? throw new AppException("Booking not found");

			// nếu cần include navigation: tự dùng repo custom hoặc context (tuỳ bạn)
			// ví dụ: _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(...)

			var contract = new Contract
			{
				Id = Guid.NewGuid(),
				Type = ContractType.Booking,
				BookingId = booking.Id,
				Status = ContractStatus.PendingSignatures,
				CreatedAt = DateTime.UtcNow
			};

			await contractRepo.AddAsync(contract);

			// tạo signer renter
			var renterSignature = new ContractSignature
			{
				Id = Guid.NewGuid(),
				ContractId = contract.Id,
				Role = ContractSignerRole.Renter,
				UserId = booking.RenterId,
				IsSigned = false
			};
			await signatureRepo.AddAsync(renterSignature);

			// tạo signer platform (staff)
			var platformSignature = new ContractSignature
			{
				Id = Guid.NewGuid(),
				ContractId = contract.Id,
				Role = ContractSignerRole.Platform,
				UserId = staffUserId,
				IsSigned = false
			};
			await signatureRepo.AddAsync(platformSignature);

			await _unitOfWork.Complete();

			return contract;
		}

		/// <summary>
		/// Nhận chữ ký base64, upload Cloudinary, update ContractSignature.
		/// Nếu tất cả đã ký => generate PDF, upload Cloudinary, lưu hash.
		/// </summary>
		public async Task<Contract> SignContractAsync(Guid contractId, ContractSignerRole role,
			string signatureBase64, Guid? userId, string? ip, string? userAgent)
		{
			var contractRepo = _unitOfWork.Repository<Contract>();
			var signatureRepo = _unitOfWork.Repository<ContractSignature>();

			// Load contract + signatures + booking (tuỳ repo bạn implement Include)
			var contract = await contractRepo.GetByIdAsync(contractId)
							 ?? throw new AppException("Contract not found");

			// nếu dùng repo generic khó Include, bạn có thể viết thêm method custom:
			// var contract = await _contractRepository.GetContractWithDetailsAsync(contractId);

			var signature = (await signatureRepo.GetAllAsync())
				.FirstOrDefault(s => s.ContractId == contractId && s.Role == role)
				?? throw new AppException("Signature slot not found for this role");

			if (signature.IsSigned)
				throw new AppException("This signer already signed");

			// decode base64
			var base64 = signatureBase64.Replace("data:image/png;base64,", string.Empty);
			var bytes = Convert.FromBase64String(base64);

			// upload Cloudinary
			var fileName = $"signature_{contractId}_{role}.png";
			var asset = await _fileStorage.UploadAsync(
				bytes,
				fileName,
				"image/png",
				contractId,
				FileOwnerType.ContractSignature,
				folder: "camrent/contracts/signatures",
				label: role.ToString());

			// update signature row
			signature.IsSigned = true;
			signature.SignedAt = DateTime.UtcNow;
			signature.SignatureAssetId = asset.Id;
			signature.SignedIp = ip;
			signature.SignedUserAgent = userAgent;

			// tạm thời DocumentHashAtSignTime sẽ set sau khi generate PDF cuối;
			// hoặc bạn có thể generate mỗi lần ký 1 bản (tùy logic)

			await _unitOfWork.Complete();

			// Reload signatures để check đủ chưa
			var allSignatures = (await signatureRepo.GetAllAsync())
				.Where(s => s.ContractId == contractId)
				.ToList();

			if (allSignatures.All(s => s.IsSigned))
			{
				// tất cả đã ký => generate contract final
				await GenerateAndUploadFinalPdfAsync(contract, allSignatures);
			}

			return contract;
		}

		/// <summary>
		/// Download PDF hợp đồng (nếu đã generate).
		/// Nếu bạn lưu ở Cloudinary dạng raw file, có thể chỉ trả url.
		/// </summary>
		public async Task<byte[]?> DownloadContractPdfAsync(Guid contractId)
		{
			var contractRepo = _unitOfWork.Repository<Contract>();
			var contract = await contractRepo.GetByIdAsync(contractId)
						   ?? throw new AppException("Contract not found");

			if (contract.FileAsset == null || string.IsNullOrEmpty(contract.FileAsset.Url))
				return null;

			using var http = new HttpClient();
			return await http.GetByteArrayAsync(contract.FileAsset.Url);
		}

		#endregion

		#region Private helpers

		private async Task GenerateAndUploadFinalPdfAsync(Contract contract, List<ContractSignature> signatures)
		{
			// Load đầy đủ navigation nếu cần: Booking, Branch, Renter, ...
			// nếu contract hiện tại chưa có => bạn phải load lại bằng repo custom

			// 1. Render PDF bytes
			byte[] pdfBytes = contract.Type switch
			{
				ContractType.Booking => await _templateService.RenderBookingContractAsync(contract),
				ContractType.Verification => await _templateService.RenderVerificationContractAsync(contract),
				_ => throw new AppException("Unsupported contract type")
			};

			// 2. Hash SHA256
			var hash = ComputeSha256(pdfBytes);

			// 3. Upload Cloudinary raw file
			var fileName = $"contract_{contract.Id}.pdf";
			var pdfAsset = await _fileStorage.UploadAsync(
				pdfBytes,
				fileName,
				"application/pdf",
				contract.Id,
				FileOwnerType.ContractDocument,
				folder: "camrent/contracts/pdfs",
				label: contract.Type.ToString());

			// 4. Update contract
			var contractRepo = _unitOfWork.Repository<Contract>();
			contract.FileAssetId = pdfAsset.Id;
			contract.FileHash = hash;
			contract.Status = ContractStatus.Signed;
			contract.SignedAt = DateTime.UtcNow;

			// 5. Option: set DocumentHashAtSignTime cho từng chữ ký
			var signatureRepo = _unitOfWork.Repository<ContractSignature>();
			foreach (var sig in signatures)
			{
				sig.DocumentHashAtSignTime = hash;
			}

			await _unitOfWork.Complete();
		}

		private static string ComputeSha256(byte[] data)
		{
			using var sha = SHA256.Create();
			var hashBytes = sha.ComputeHash(data);
			return Convert.ToHexString(hashBytes); // ABCDEF...
		}

		#endregion
	}
}
