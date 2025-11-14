using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.Services
{
	public class CloudinaryFileStorageService : IFileStorageService
	{
		private readonly Cloudinary _cloudinary;
		private readonly IUnitOfWork _unitOfWork;

		public CloudinaryFileStorageService(IOptions<CloudinarySettings> options, IUnitOfWork unitOfWork)
		{
			var settings = options.Value;
			var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
			_cloudinary = new Cloudinary(account);
			_unitOfWork = unitOfWork;
		}

		public async Task<FileAsset> UploadAsync(IFormFile file, Guid ownerId,FileOwnerType ownerType, string? folder = null, string? label = null)
		{
			await using var stream = file.OpenReadStream();

			var uploadParams = new ImageUploadParams
			{
				File = new FileDescription(file.FileName, stream),
				Folder = folder,             // ví dụ: "camrent/cameras"
				PublicId = null,             // để null cho Cloudinary tự sinh
				UseFilename = true,          // giữ tên file
				UniqueFilename = true,       // tránh trùng
				Overwrite = false
			};

			var result = await _cloudinary.UploadAsync(uploadParams);

			if (result.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new Exception($"Cloudinary upload failed: {result.Error?.Message}");
			}

			var asset = new FileAsset
			{
				Url = result.SecureUrl?.ToString() ?? string.Empty,
				ContentType = file.ContentType,
				SizeBytes = file.Length,
				Label = label,
				Provider = "Cloudinary",
				ProviderKey = result.PublicId,       // rất quan trọng để delete sau này
				OwnerId = ownerId,
				OwnerType = ownerType
			};
			await _unitOfWork.Repository<FileAsset>().AddAsync(asset);
			await _unitOfWork.Complete();
			return asset;
		}

		public async Task DeleteAsync(string providerKey)
		{
			var delParams = new DeletionParams(providerKey);
			var result = await _cloudinary.DestroyAsync(delParams);

			// Tùy bạn muốn check result.Result == "ok" hay bỏ qua
			if(result.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new Exception($"Cloudinary delete failed: {result.Error?.Message}");
			}
		}
	}
}
