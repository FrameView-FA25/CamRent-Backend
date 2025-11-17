using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.AccessoryModel;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Consumes("multipart/form-data")]
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
		[SwaggerOperation(Summary = "Cập nhật phụ kiện", Description = "Cập nhật thông tin phụ kiện theo id. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> UpdateAccessory(Guid id, [FromBody] AccessoryRequest accessoryUpdateModel)
		{
			var existingAccessory = await _accessoryService.GetAccessoryByIdAsync(id);
			if (existingAccessory == null)
			{
				return NotFound();
			}
			var accessoryToUpdate = _mapper.Map<Accessory>(accessoryUpdateModel);
			accessoryToUpdate.Id = id;
			var result = await _accessoryService.UpdateAccessoryAsync(accessoryToUpdate);
			return result > 0 ? Ok(new { Message = "Cập nhật phụ kiện thành công." }) : BadRequest(new { Message = "Cập nhật phụ kiện thất bại." });
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


