using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Http;
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
using static CamRent_Application.DTOs.UserProfileDTO;

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

		public async Task<UserProfileResponse> GetUserProfileWithBranchAsync(Guid id)
		{
			var users = await _unitOfWork.Repository<User>()
				.ListAsync(p => p.Id == id, include: q => q.Include(u => u.Roles).Include(u => u.SignatureAsset).Include(u => u.Avatar));
			var user = users.FirstOrDefault() ?? throw new InvalidOperationException("User not found");

			var response = new UserProfileResponse
			{
				Id = user.Id,
				Email = user.Email,
				Phone = user.Phone,
				FullName = user.FullName,
				Address = user.Address,
				Status = user.Status,
				BankAccountNumber = user.BankAccountNumber,
				BankName = user.BankName,
				BankAccountName = user.BankAccountName,
				SignatureAssetId = user.SignatureAssetId,
				AvatarId = user.AvatarId,
				AvatarUrl = user.Avatar?.Url,
				SignatureUrl = user.SignatureAsset?.Url,
				Roles = user.Roles.Select(r => r.Role.ToString()).ToList()
			};

			// Lấy thông tin chi nhánh nếu user là Staff hoặc BranchManager
			var userRoles = user.Roles.Select(r => r.Role).ToList();
			if (userRoles.Contains(UserRole.Staff) || userRoles.Contains(UserRole.BranchManager))
			{
				Branch? branch = null;

				// Nếu là BranchManager, lấy branch từ ManagerId
				if (userRoles.Contains(UserRole.BranchManager))
				{
					branch = (await _unitOfWork.Repository<Branch>()
						.ListAsync(filter: b => b.ManagerId == id))
						.FirstOrDefault();
				}

				// Nếu là Staff hoặc chưa tìm thấy branch (có thể là Staff), lấy từ UserBranchMembership
				if (branch == null)
				{
					var membership = (await _unitOfWork.Repository<UserBranchMembership>()
						.ListAsync(
							filter: m => m.UserId == id,
							include: m => m.Include(m => m.Branch)))
						.FirstOrDefault();

					if (membership != null)
					{
						branch = membership.Branch;
					}
				}

				if (branch != null)
				{
					response.Branch = new BranchInfo
					{
						Id = branch.Id,
						Name = branch.Name,
						Address = branch.Address,
						IsManager = userRoles.Contains(UserRole.BranchManager)
					};
				}
			}

			return response;
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

		public async Task UpdateAccountAsync(Guid userId, string? email, string? fullName, string? phone, string? country, string? province, string? district)
		{
			var user = (await _unitOfWork.Repository<User>().ListAsync(p => p.Id == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");

			// Email: nếu đổi thì update cả NormalizedEmail
			if (!string.IsNullOrWhiteSpace(email) && !email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
			{
				var trimmed = email.Trim();
				// Optional: check trùng email
				var exists = await _unitOfWork.Repository<User>()
					.ListAsync(u => u.Email == trimmed && u.Id != userId);
				if (exists.Any())
					throw new InvalidOperationException("Email đã được sử dụng bởi tài khoản khác.");

				user.Email = trimmed;
				user.NormalizedEmail = trimmed.ToUpperInvariant();
			}

			if (!string.IsNullOrWhiteSpace(fullName))
				user.FullName = fullName.Trim();

			if (!string.IsNullOrWhiteSpace(phone))
				user.Phone = phone.Trim();

			// Địa chỉ: nếu có bất kỳ field nào được gửi lên thì cập nhật
			if (!string.IsNullOrWhiteSpace(country)
				|| !string.IsNullOrWhiteSpace(province)
				|| !string.IsNullOrWhiteSpace(district))
			{
				user.Address ??= new Address();
				if (!string.IsNullOrWhiteSpace(country)) user.Address.Country = country!;
				if (!string.IsNullOrWhiteSpace(province)) user.Address.Province = province!;
				if (!string.IsNullOrWhiteSpace(district)) user.Address.District = district!;
			}

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

		public async Task<(Guid assetId, string url)> UpdateAvatarAsync(Guid userId, IFormFile avatarFile, CancellationToken ct = default)
		{
			if (avatarFile == null || avatarFile.Length <= 0)
				throw new InvalidOperationException("Avatar file is required");

			var userRepo = _unitOfWork.Repository<User>();
			var user = (await userRepo.ListAsync(p => p.Id == userId, include: q => q.Include(u => u.Avatar))).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");

			// xoá avatar cũ (nếu có)
			if (user.AvatarId.HasValue)
			{
				await _fileStorage.DeleteByAssetIdAsync(user.AvatarId.Value);
				user.AvatarId = null;
			}

			// upload avatar mới
			var asset = await _fileStorage.UploadAsync(
				avatarFile,
				ownerId: userId,
				ownerType: FileOwnerType.UserAvatar,
				folder: "camrent/users/avatars",
				label: "UserAvatar");

			user.AvatarId = asset.Id;
			await userRepo.UpdateAsync(user);
			await _unitOfWork.Complete();

			return (asset.Id, asset.Url);
		}
	}
}
