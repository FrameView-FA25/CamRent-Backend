using AutoMapper;
using CamRent_Api.Models;
using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Api.Models.AccessoryModel;
using static CamRent_Application.DTOs.AccessoryDTO;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	
	public class AccessoriesController : ControllerBase
	{
		private readonly IAccessoryService _accessoryService;
		private readonly IMapper _mapper;
		private readonly IFileStorageService _fileStorageService;

		public AccessoriesController(IAccessoryService accessoryService, IMapper mapper, IFileStorageService fileStorageService)
		{
			_accessoryService = accessoryService;
			_mapper = mapper;
			_fileStorageService = fileStorageService;
		}

		[HttpGet]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Lấy danh sách phụ kiện", Description = "Trả về danh sách phụ kiện (phân trang). Hỗ trợ tìm kiếm và sắp xếp. Quyền: Công khai")]
		public async Task<IActionResult> GetAllAccessories([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, [FromQuery] string? sortBy = "createdAt", [FromQuery] string sortDir = "desc")
		{
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 1, 100);
			var accessories = await _accessoryService.GetAllAccessoriesAsync();
			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLowerInvariant();
				accessories = accessories.Where(a => ($"{a.Brand} {a.Model} {a.Variant}").ToLower().Contains(term)).ToList();
			}
			IEnumerable<dynamic> sorted = accessories;
			if (string.Equals(sortBy, "brand", StringComparison.OrdinalIgnoreCase))
				sorted = (sortDir == "asc" ? accessories.OrderBy(a => a.Brand) : accessories.OrderByDescending(a => a.Brand));
			else if (string.Equals(sortBy, "model", StringComparison.OrdinalIgnoreCase))
				sorted = (sortDir == "asc" ? accessories.OrderBy(a => a.Model) : accessories.OrderByDescending(a => a.Model));
			else
				sorted = (sortDir == "asc" ? accessories.OrderBy(a => a.Id) : accessories.OrderByDescending(a => a.Id));

			var total = sorted.Count();
			var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return Ok(new { page, pageSize, total, items });
		}
		[HttpGet("{id}")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Lấy phụ kiện theo id", Description = "Trả về thông tin phụ kiện theo id. Quyền: Công khai")]
		public async Task<IActionResult> GetAccessoryById(Guid id)
		{
			var accessory = await _accessoryService.GetAccessoryByIdAsync(id);
			if (accessory == null)
			{
				return NotFound();
			}
			return Ok(accessory);
		}
		[HttpGet("GetAccessoriesByOwnerId")]
		[SwaggerOperation(Summary = "Lấy phụ kiện của chủ sở hữu", Description = "Trả về các phụ kiện thuộc về người dùng đang xác thực. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> GetAccessoriesByOwnerId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrEmpty(userId))
			{
				return Forbid();
			}

			var accessories = await _accessoryService.GetAccessoriesByOwnerIdAsync(Guid.Parse(userId));
			return Ok(accessories);
		}

		[Authorize(Policy = "Owner")]
		[HttpPost]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Tạo phụ kiện", Description = "Tạo mới một phụ kiện. Chấp nhận multipart/form-data kèm file media. Chủ sở hữu lấy từ người dùng đang xác thực. Quyền: Owner, Admin")]
		public async Task<IActionResult> CreateAccessory([FromForm] AccessoryRequest accessoryCreateModel)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
				  ?? User.FindFirst("sub")?.Value
				  ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var accessory = _mapper.Map<Accessory>(accessoryCreateModel);
			accessory.OwnerUserId = Guid.Parse(userId);

			var result = await _accessoryService.CreateAccessoryAsync(accessory);

			accessory.Media ??= new List<FileAsset>();

			if (accessoryCreateModel.MediaFiles != null)
			{
				foreach (var file in accessoryCreateModel.MediaFiles)
				{
					if (file == null || file.Length <= 0) continue;

					var asset = await _fileStorageService.UploadAsync(
						file,
						ownerId: accessory.Id,
						ownerType: FileOwnerType.Accessory,
						folder: $"camrent/accessories/{accessory.Id}",
						label: $"{accessory.Brand} {accessory.Model}"
					);
					accessory.Media.Add(asset);
				}
			}

			if (result <= 0)
			{
				return BadRequest("Tạo phụ kiện thất bại.");
			}
			return Ok(new { Message = "Tạo phụ kiện thành công." });
		}

		[HttpPut("{id}")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Cập nhật phụ kiện", Description = "Cập nhật thông tin phụ kiện theo id. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> UpdateAccessory([FromForm] UpdateAccessoryRequest model)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrEmpty(userId))
				return Unauthorized();
			var result = await _accessoryService.UpdateAccessoryAsync(model, Guid.Parse(userId));
			// 1) Lấy entity thật từ DB
			var existing = await _accessoryService.GetAccessoryByIdAsync(model.Id);
			if (existing == null)
				return NotFound();

			if (existing.OwnerUserId != Guid.Parse(userId))
				return Forbid();

			existing.Media ??= new List<FileAssetDTO>();

			// 3) Handle remove old media
			if (model.RemoveMediaIds != null && model.RemoveMediaIds.Any())
			{
				var toRemove = existing.Media
					.Where(m => model.RemoveMediaIds.Contains(m.Id))
					.ToList();

				foreach (var old in toRemove)
				{
					await _fileStorageService.DeleteByAssetIdAsync(old.Id);
					existing.Media.Remove(old);
				}
			}

			// 4) Upload new media files (giống hệt CreateAccessory)
			if (model.MediaFiles != null)
			{
				foreach (var file in model.MediaFiles)
				{
					if (file == null || file.Length <= 0) continue;

					var asset = await _fileStorageService.UploadAsync(
						file,
						ownerId: existing.Id,
						ownerType: FileOwnerType.Accessory,
						folder: $"camrent/accessories/{existing.Id}",
						label: $"{existing.Brand} {existing.Model}"
					);
				}
			}
			

			if (result <= 0)
				return BadRequest(new { Message = "Cập nhật phụ kiện thất bại." });

			return Ok(new { Message = "Cập nhật phụ kiện thành công." });
		}


		[HttpDelete("{id}")]
		[SwaggerOperation(Summary = "Xóa phụ kiện", Description = "Xóa phụ kiện theo id. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> DeleteAccessory(Guid id)
		{
			var existingAccessory = await _accessoryService.GetAccessoryByIdAsync(id);
			if (existingAccessory == null)
			{
				return NotFound();
			}
			var result = await _accessoryService.DeleteAccessoryAsync(id);
			return result > 0 ? Ok(new { Message = "Xóa phụ kiện thành công." }) : BadRequest(new { Message = "Xóa phụ kiện thất bại." });
		}
	}
}


