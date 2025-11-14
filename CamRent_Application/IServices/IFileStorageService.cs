using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IFileStorageService
	{
		Task<FileAsset> UploadAsync(IFormFile file, Guid ownerId, FileOwnerType ownerType, string? folder = null, string? label = null);
		Task DeleteAsync(string providerKey);

	}
}
