using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;		
using System.Diagnostics.Contracts;
using System.IdentityModel.Tokens.Jwt;     
using System.Security.Claims;             
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static CamRent_Application.DTOs.AuthDTO;

namespace CamRent_Application.Services
{
	public class UserService : IUserService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IFileStorageService _fileStorage;
		private readonly IContractService _contractService;
		public UserService(IUnitOfWork uow, IFileStorageService fileStorage, IContractService contractService)
		{
			_unitOfWork = uow;
			_fileStorage = fileStorage;
			_contractService = contractService;
		}

		public async Task<int> DeleteUser(Guid id)
		{
			await _unitOfWork.Repository<User>().DeleteAsync(id);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<List<User>> GetAllUsers()
		{
			var result = await _unitOfWork.Repository<User>()
				.ListAsync(include: q => q.Include(u => u.Roles));
			return result.ToList();
		}

		public async Task<User> GetUserProfileById(Guid id)
		{
			var users = await _unitOfWork.Repository<User>()
				.ListAsync(p => p.Id == id, include: q => q.Include(u => u.Roles).Include(u => u.SignatureAsset).Include(u => u.Avatar));
			var user = users.FirstOrDefault() ?? throw new InvalidOperationException("User not found");
			return user;
		}

		public async Task<Guid> GetUserIdByManagerId(Guid managerId)
		{
			var user = await _unitOfWork.Repository<User>().ListAsync();
			return user!.FirstOrDefault(u => u.CreatedByUserId == managerId)?.Id ?? Guid.Empty;
		}

		public async Task<int> UpdateUser(User user)
		{
			await _unitOfWork.Repository<User>().UpdateAsync(user);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<(Guid id, string? bankNo, string? bankName, string? bankAccName)> GetProfileAsync(Guid userId)
		{
			var user = (await _unitOfWork.Repository<User>().ListAsync(p => p.Id == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");
			return (user.Id, user.BankAccountNumber, user.BankName, user.BankAccountName);
		}

		public async Task UpdateProfileAsync(Guid userId, string? bankNo, string? bankName, string? bankAccName)
		{
			var user = (await _unitOfWork.Repository<User>().ListAsync(p => p.Id == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");
			user.BankAccountNumber = bankNo ?? user.BankAccountNumber;
			user.BankName = bankName ?? user.BankName;
			user.BankAccountName = bankAccName ?? user.BankAccountName;
			await _unitOfWork.Repository<User>().UpdateAsync(user);
			await _unitOfWork.Complete();
		}

		public async Task<int> UpdateUserSignAsync(Guid userId, string signatureBase64)
		{
			var userRepo = _unitOfWork.Repository<User>();
			var fileAssetRepo = _unitOfWork.Repository<FileAsset>();
			var contractSignatureRepo = _unitOfWork.Repository<ContractSignature>();

			var user = (await userRepo.ListAsync(p => p.Id == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");

			if (string.IsNullOrWhiteSpace(signatureBase64))
				throw new InvalidOperationException("Signature data is empty");
			// Decode base64
			var cleaned = signatureBase64;
			if (cleaned.StartsWith("data:image"))
			{
				var idx = cleaned.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
				if (idx >= 0)
					cleaned = cleaned[(idx + "base64,".Length)..];
			}
			var bytes = Convert.FromBase64String(cleaned);
			var fileName = $"user_signature_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";

			// Upload chữ ký mới
			var asset = await _fileStorage.UploadAsync(
				bytes,
				fileName,
				"image/png",
				userId,
				FileOwnerType.UserSignature,
				folder: "camrent/users/signatures",
				label: "UserSignature");

			// Cập nhật user dùng chữ ký mới
			user.SignatureAssetId = asset.Id;
			await userRepo.UpdateAsync(user);

			// Cập nhật chữ ký trên các ContractSignature đang pending (như code của bạn)
			var managerSignatures = (await contractSignatureRepo.ListAsync(
					include: q => q.Include(c => c.Contract),
					filter: s =>
						s.UserId == userId &&
						s.Contract.Status == ContractStatus.PendingSignatures &&
						s.Contract.VerificationId == null
				))
				.ToList();

			foreach (var sig in managerSignatures)
			{
				sig.SignatureAssetId = asset.Id;
				sig.IsSigned = true;
				await contractSignatureRepo.UpdateAsync(sig);
			}

			var result = await _unitOfWork.Complete();

			// Sau khi save xong mới generate final PDF nếu tất cả đã ký
			foreach (var sig in managerSignatures)
			{
				var allSignatures = (await contractSignatureRepo.GetAllAsync())
					.Where(s => s.ContractId == sig.ContractId)
					.ToList();

				if (allSignatures.All(s => s.IsSigned))
				{
					await _contractService.GenerateAndUploadFinalPdfAsync(sig.ContractId, allSignatures);
				}
			}

			return result;
		}
	}
}
