using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;     
using System.Security.Claims;             
using Microsoft.IdentityModel.Tokens;		
using System.Text;
using static CamRent_Application.DTOs.AuthDTO;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace CamRent_Application.Services
{
	public class UserService : IUserService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IFileStorageService _fileStorage;
		public UserService(IUnitOfWork uow, IFileStorageService fileStorage)
		{
			_unitOfWork = uow;
			_fileStorage = fileStorage;
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

		public async Task UpdateUserSignAsync(Guid userId, string signatureBase64)
		{
			var userRepo = _unitOfWork.Repository<User>();
			var fileAssetRepo = _unitOfWork.Repository<FileAsset>();
			var contractSignatureRepo = _unitOfWork.Repository<ContractSignature>();

			var user = (await userRepo.ListAsync(p => p.Id == userId)).FirstOrDefault()
				?? throw new InvalidOperationException("User not found");

			if (string.IsNullOrWhiteSpace(signatureBase64))
				throw new InvalidOperationException("Signature data is empty");

			// 1) Decode base64 (có thể có prefix "data:image/png;base64,....")
			var cleaned = signatureBase64;
			if (cleaned.StartsWith("data:image"))
			{
				var idx = cleaned.IndexOf("base64,");
				if (idx >= 0)
					cleaned = cleaned.Substring(idx + "base64,".Length);
			}

			var bytes = Convert.FromBase64String(cleaned);

			var fileName = $"user_signature_{userId}.png";

			// 2) Upload lên Cloudinary (tùy theo chữ ký IFileStorage của bạn)
			var asset = await _fileStorage.UploadAsync(
				bytes,
				fileName,
				"image/png",
				userId,
				FileOwnerType.UserSignature,     // nếu bạn có enum này, hoặc đổi lại cho phù hợp
				folder: "camrent/users/signatures",
				label: "UserSignature");


			// 4) Gán vào user
			user.SignatureAssetId = asset.Id;
			await userRepo.UpdateAsync(user);

			// 5) Cập nhật tất cả ContractSignature liên quan đến user này
			//    (giả sử ContractSignature có field SignedByUserId)
			var managerSignatures = await contractSignatureRepo.ListAsync(s =>
				s.UserId == userId && s.Contract.Status == ContractStatus.PendingSignatures);

			foreach (var sig in managerSignatures)
			{	
				sig.SignatureAssetId = asset.Id;
				// Không đổi SignedAt, vì thời điểm ký vẫn giữ nguyên,
				// chỉ thay hình chữ ký.
				sig.IsSigned = true;
				await contractSignatureRepo.UpdateAsync(sig);
			}

			await _unitOfWork.Complete();

			// Nếu bạn muốn regenerate lại PDF cho các contract đã ký,
			// có thể làm thêm bước 6 (ở dưới).
		}

	}
}
