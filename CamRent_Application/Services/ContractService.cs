using AutoMapper;
using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using PayOS.Exceptions;
using System.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static CamRent_Application.DTOs.ContractDTO;

namespace CamRent_Application.Services
{
	public class ContractService : IContractService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IContractTemplateService _templateService;
		private readonly IFileStorageService _fileStorage;
		private readonly IMapper _mapper;

		public ContractService(
			IUnitOfWork unitOfWork,
			IContractTemplateService templateService,
			IFileStorageService fileStorage,
			IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_templateService = templateService;
			_fileStorage = fileStorage;
			_mapper = mapper;
		}

		public async Task<List<ContractResponse>> GetContractsAsync()
		{
			var repo = _unitOfWork.Repository<Contract>();

			// tuỳ implementation IGenericRepository của anh, mình giả sử có AsQueryable()
			var query = await repo.ListAsync( include: q => q
				.Include(c => c.Branch)
				.Include(c => c.Signatures).ThenInclude(s => s.User));

			var contracts = query.ToList();
			return _mapper.Map<List<ContractResponse>>(contracts);
		}

		public async Task<ContractResponse?> GetContractByIdAsync(Guid contractId)
		{
			var repo = _unitOfWork.Repository<Contract>();

			var contract = (await repo.ListAsync(include: q => q
				.Include(c => c.Branch)
				.Include(c => c.Signatures).ThenInclude(s => s.User)))
				.FirstOrDefault(c => c.Id == contractId);

			if (contract == null)
				return null;

			return _mapper.Map<ContractResponse>(contract);
		}


		public async Task<Contract> CreateBookingContractAsync(Guid bookingId, Guid staffUserId)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();
			var contractRepo = _unitOfWork.Repository<Contract>();
			var signatureRepo = _unitOfWork.Repository<ContractSignature>();

			var booking = (await bookingRepo.ListAsync(include: q => q
							.Include(b => b.Branch)
								.ThenInclude(br => br.Manager)))
						 .FirstOrDefault(b => b.Id == bookingId)
						 ?? throw new AppException("Booking not found");

			if (booking.BranchId == null || booking.Branch == null)
				throw new AppException("Booking does not have a branch assigned");

			if (booking.Branch.ManagerId == null || booking.Branch.Manager == null)
				throw new AppException("Branch does not have a manager configured");

			var hasPending = await contractRepo.FirstOrDefaultAsync(c =>
				c.BookingId == bookingId);

			if (hasPending != null)
			{
				return hasPending;
			}

			var contract = new Contract
			{
				Id = Guid.NewGuid(),
				Type = ContractType.Booking,
				BookingId = booking.Id,
				BranchId = booking.BranchId,
				Status = ContractStatus.PendingSignatures,
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = staffUserId
			};

			await contractRepo.AddAsync(contract);

			// signer renter
			var renterSignature = new ContractSignature
			{
				Id = Guid.NewGuid(),
				ContractId = contract.Id,
				Role = ContractSignerRole.Renter,
				UserId = booking.RenterId,
				IsSigned = false
			};
			await signatureRepo.AddAsync(renterSignature);

			// signer platform (manager)
			var signManagerId = booking.Branch.Manager.SignatureAssetId;
			var isSignedByManager = signManagerId != null;

			var platformSignature = new ContractSignature
			{
				Id = Guid.NewGuid(),
				ContractId = contract.Id,
				Role = ContractSignerRole.Platform,
				UserId = booking.Branch.ManagerId,
				SignatureAssetId = signManagerId,
				IsSigned = isSignedByManager
			};
			await signatureRepo.AddAsync(platformSignature);

			await _unitOfWork.Complete();

			return contract;
		}


		public async Task<Contract> CreateVerificationContractAsync(Guid verificationId, Guid staffUserId)
		{
			var contractRepo = _unitOfWork.Repository<Contract>();
			var signatureRepo = _unitOfWork.Repository<ContractSignature>();

			// 1. Kiểm tra owner tồn tại
			var verification = (await _unitOfWork.Repository<VerificationRequest>().ListAsync(include: q => q
				.Include(v => v.Branch).ThenInclude(v => v.Manager))).FirstOrDefault(v => v.Id == verificationId)
				?? throw new AppException("Verification not found");
			var check = await contractRepo.AnyAsync(c => c.VerificationId == verificationId && c.Status == ContractStatus.PendingSignatures);
			if (check)
				throw new AppException("A pending contract already exists for this verification");
			// 2. Tạo contract Verification
			var contract = new Contract
			{
				Id = Guid.NewGuid(),
				Type = ContractType.Verification,
				VerificationId = verificationId,
				BranchId = verification.BranchId,
				Status = ContractStatus.PendingSignatures,
				CreatedAt = DateTime.UtcNow
				
				// nếu bạn muốn gắn Branch nào đó thì set BranchId ở đây
			};

			await contractRepo.AddAsync(contract);

			// 3. Tạo slot chữ ký cho Owner
			var ownerSignature = new ContractSignature
			{
				Id = Guid.NewGuid(),
				ContractId = contract.Id,
				Role = ContractSignerRole.Owner,
				UserId = verification.CreatedByUserId,
				IsSigned = false
			};
			await signatureRepo.AddAsync(ownerSignature);
			// 4. Tạo slot chữ ký cho CamRent (Platform)
			var platformSignature = new ContractSignature
			{
				Id = Guid.NewGuid(),
				ContractId = contract.Id,
				Role = ContractSignerRole.Platform,
				UserId = verification.Branch.ManagerId, 
				IsSigned = false
			};
			await signatureRepo.AddAsync(platformSignature);

			// 5. Lưu DB
			await _unitOfWork.Complete();

			return contract;
		}


		/// <summary>
		/// Nhận chữ ký base64, upload Cloudinary, update ContractSignature.
		/// Nếu tất cả đã ký => generate PDF, upload Cloudinary, lưu hash.
		/// </summary>
		public async Task<Contract> SignContractAsync(
			Guid contractId,
			ContractSignerRole role,
			string signatureBase64,
			Guid? userId,
			string? ip,
			string? userAgent)
		{
			var contractRepo = _unitOfWork.Repository<Contract>();
			var signatureRepo = _unitOfWork.Repository<ContractSignature>();

			var contract = await contractRepo.GetByIdAsync(contractId)
							 ?? throw new AppException("Contract not found");

			var signature = await signatureRepo.FirstOrDefaultAsync(s => s.ContractId == contractId && s.Role == role)
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

			if (asset == null)
				throw new AppException("Upload signature failed");

			// update signature row
			signature.IsSigned = true;
			if (role == ContractSignerRole.Renter && userId != null)
			{
				signature.UserId = userId;
				signature.IsSigned = false; // đảm bảo renter ký đúng user
			}
			signature.SignedAt = DateTime.UtcNow;
			signature.SignatureAssetId = asset.Id;
			signature.SignedIp = ip;
			signature.SignedUserAgent = userAgent;

			var allSignatures = (await signatureRepo.GetAllAsync())
				.Where(s => s.ContractId == contractId)
				.ToList();

			if (allSignatures.All(s => s.IsSigned))
			{
				contract.Status = ContractStatus.Signed;
				if(role == ContractSignerRole.Owner)
				{
					try
					{
						await GenerateAndUploadFinalPdfAsync(contract.Id, allSignatures);
					}
					catch (Exception ex)
					{
						// Log lỗi nhưng KHÔNG làm hỏng việc ký
						// Có thể set trạng thái riêng nếu muốn, ví dụ:
						// contract.Status = ContractStatus.SignedButPdfFailed;
						// await contractRepo.UpdateAsync(contract);
						// await _unitOfWork.Complete();
					}
				}
			}

			await _unitOfWork.Complete(); // 🟢 TỚI ĐÂY CHẮC CHẮN ĐÃ KÝ

			

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



		public async Task GenerateAndUploadFinalPdfAsync(Guid contractId, List<ContractSignature> signatures)
		{
			// Load đầy đủ navigation nếu cần: Booking, Branch, Renter, ...
			// nếu contract hiện tại chưa có => bạn phải load lại bằng repo custom
			var contractRepo = _unitOfWork.Repository<Contract>();

			// Load contract đầy đủ từ DB
			var contract = await contractRepo.GetByIdAsync(contractId);

			if (contract == null)
				throw new AppException($"Contract {contractId} not found");
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
			contract.FileAssetId = pdfAsset.Id;
			contract.FileHash = hash;
			contract.Status = ContractStatus.Completed;
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

		public async Task<Contract?> GetByIdAsync(Guid contractId)
		{
			var contract = await _unitOfWork.Repository<Contract>().ListAsync(
				filter: c => c.Id == contractId,
				include: q => q
					.Include(c => c.Booking).ThenInclude(b => b.Renter)
					.Include(c => c.Booking)
					.Include(c => c.Booking).ThenInclude(b => b.Items!).ThenInclude(i => i.Camera)
					.Include(c => c.Booking).ThenInclude(b => b.Items!).ThenInclude(i => i.Accessory)
					.Include(c => c.Booking).ThenInclude(b => b.Items!).ThenInclude(i => i.Combo)
					.Include(c => c.FileAsset)
					.Include(c => c.Verification).ThenInclude(v => v.Owner)
					.Include(c => c.Verification)
					.Include(c => c.Verification).ThenInclude(v => v.Items!).ThenInclude(i => i.Camera)
					.Include(c => c.Verification).ThenInclude(v => v.Items!).ThenInclude(i => i.Accessory)
					.Include(c => c.Signatures).ThenInclude(s => s.SignatureAsset)
					.Include(c => c.Branch)
				);
			return contract.FirstOrDefault();
		}

		public async Task DeleteBookingContractAsync(Guid bookingId)
		{
			var contractRepo = _unitOfWork.Repository<Contract>();
			var contract = await contractRepo.FirstOrDefaultAsync(c => c.BookingId == bookingId);
			if (contract == null)
				return;

			await contractRepo.DeleteAsync(contract.Id);
			await _unitOfWork.Complete();
		}

		public async Task UpdateStatusContractSignatureByBookingIdAsync(Guid booking)
		{
			var contractRepo = _unitOfWork.Repository<Contract>();
			var signatureRepo = _unitOfWork.Repository<ContractSignature>();
			var contract = await contractRepo.FirstOrDefaultAsync(c => c.BookingId == booking);
			var signature = await signatureRepo.FirstOrDefaultAsync(s => s.ContractId == contract.Id && s.Role == ContractSignerRole.Renter);
			if (signature != null)
			{
				signature.IsSigned = true;
				await signatureRepo.UpdateAsync(signature);
			}
		}
	}
}
