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
using System.IO;
using System.Linq;
using System.Net;
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

		public async Task<FileAsset> UploadAsync(byte[] content, string fileName, string contentType, Guid ownerId, FileOwnerType ownerType, string? folder = null, string? label = null)
		{
			await using var stream = new MemoryStream(content);

			var uploadParams = new RawUploadParams
			{
				File = new FileDescription(fileName, stream),
				Folder = folder,
				UseFilename = true,
				UniqueFilename = true,
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
				ContentType = contentType,
				SizeBytes = content.LongLength,
				Label = label,
				Provider = "Cloudinary",
				ProviderKey = result.PublicId,
				OwnerId = ownerId,
				OwnerType = ownerType
			};
			await _unitOfWork.Repository<FileAsset>().AddAsync(asset);
			await _unitOfWork.Complete();
			return asset;
		}

		public async Task DeleteByAssetIdAsync(Guid fileAssetId)
		{
			var asset = await _unitOfWork.Repository<FileAsset>().GetByIdAsync(fileAssetId);
			if (asset == null)
			{
				// không tìm thấy -> không làm gì
				return;
			}

			if (!string.IsNullOrEmpty(asset.ProviderKey))
			{
				var delParams = new DeletionParams(asset.ProviderKey);
				var result = await _cloudinary.DestroyAsync(delParams);

				var statusOk = result.StatusCode == HttpStatusCode.OK;
				var res = (result.Result ?? string.Empty).ToLowerInvariant();

				// Cloudinary có thể trả "ok", "not_found", hoặc "not found"
				var isAcceptable =
					res == "ok" ||
					res.Contains("not found");

				if (!statusOk || !isAcceptable)
				{
					throw new Exception(
						$"Cloudinary delete failed for ProviderKey={asset.ProviderKey}: {result.Error?.Message ?? result.Result}");
				}
			}

			// Xóa bản ghi DB
			await _unitOfWork.Repository<FileAsset>().DeleteAsync(asset.Id);

			await _unitOfWork.Complete();
		}

	}
}
