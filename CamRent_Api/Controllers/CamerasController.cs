using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.CameraModel;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Consumes("multipart/form-data")]
	public class CamerasController : ControllerBase
	{
		private readonly ICameraService _cameraService;
		private readonly IMapper _autoMapper;
		private readonly IFileStorageService _fileStorageService;
		public CamerasController(ICameraService cameraService, IMapper autoMapper, IFileStorageService fileStorageService)
		{
			_cameraService = cameraService;
			_autoMapper = autoMapper;
			_fileStorageService = fileStorageService;
		}

		[HttpGet]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Lấy danh sách camera", Description = "Trả về danh sách camera (phân trang). Hỗ trợ tìm kiếm và sắp xếp. Quyền: Công khai")]
		public async Task<IActionResult> GetAllCameras([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, [FromQuery] string? sortBy = "createdAt", [FromQuery] string sortDir = "desc")
		{
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 1, 100);
			var cameras = await _cameraService.GetAllAsync();
			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLowerInvariant();
				cameras = cameras.Where(c => ($"{c.Brand} {c.Model} {c.Variant}").ToLower().Contains(term)).ToList();
			}
			IEnumerable<dynamic> sorted = cameras;
			if (string.Equals(sortBy, "brand", StringComparison.OrdinalIgnoreCase))
				sorted = (sortDir == "asc" ? cameras.OrderBy(c => c.Brand) : cameras.OrderByDescending(c => c.Brand));
			else if (string.Equals(sortBy, "model", StringComparison.OrdinalIgnoreCase))
				sorted = (sortDir == "asc" ? cameras.OrderBy(c => c.Model) : cameras.OrderByDescending(c => c.Model));
			else
				sorted = (sortDir == "asc" ? cameras.OrderBy(c => c.Id) : cameras.OrderByDescending(c => c.Id));

			var total = sorted.Count();
			var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return Ok(new { page, pageSize, total, items });
		}

		// Camera theo chi nhánh mà Manager quản lý
		[HttpGet("my-branch")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Danh sách camera của chi nhánh manager", Description = "Trả về danh sách camera thuộc chi nhánh mà BranchManager đang quản lý.")]
		public async Task<IActionResult> GetCamerasForMyBranch([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
		{
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 1, 100);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var managerId = Guid.Parse(userId);
			var cameras = await _cameraService.GetByBranchManagerAsync(managerId);

			var total = cameras.Count;
			var items = cameras.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return Ok(new { page, pageSize, total, items });
		}

		[HttpGet("{id:guid}")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Lấy camera theo id", Description = "Trả về thông tin camera theo id. Quyền: Công khai")]
		public async Task<IActionResult> GetCameraById(Guid id)
		{
			var camera = await _cameraService.GetByIdAsync(id);
			if (camera == null)
			{
				return NotFound();
			}
			return Ok(camera);
		}
		[HttpGet("GetCamerasByOwnerId")]
		[SwaggerOperation(Summary = "Lấy camera của chủ sở hữu", Description = "Trả về các camera thuộc về người dùng đang xác thực. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> GetCamerasByOwnerId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var cameras = await _cameraService.GetCamerasByOwnerIdAsync(Guid.Parse(userId));
			return Ok(cameras);
		}

		[HttpPost]
		[Authorize(Policy = "Owner")]
		[SwaggerOperation(Summary = "Tạo camera", Description = "Tạo mới camera. Chấp nhận multipart/form-data kèm file media. Quyền: Owner, Admin")]
		public async Task<IActionResult> CreateCamera([FromForm] CameraRequest cameraRequest)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrEmpty(userId))
			{
				return Unauthorized();
			}

			var camera = _autoMapper.Map<Camera>(cameraRequest);
			camera.OwnerUserId = Guid.Parse(userId);
			var result = await _cameraService.CreateAsync(camera);

			camera.Media ??= new List<FileAsset>();

			if (cameraRequest.MediaFiles != null)
			{
				foreach (var file in cameraRequest.MediaFiles)
				{
					if (file == null || file.Length <= 0) continue;

					var asset = await _fileStorageService.UploadAsync(
						file,
						ownerId: camera.Id,
						ownerType: FileOwnerType.Camera,
						folder: $"camrent/cameras/{camera.Id}",
						label: $"{camera.Brand} {camera.Model}"
					);
					camera.Media.Add(asset);
				}
			}

			if (result <= 0)
			{
				return BadRequest("Tạo camera thất bại.");
			}
			return Ok(new { Message = "Tạo camera thành công." });
		}

		[HttpPut("{id:guid}")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Cập nhật camera", Description = "Cập nhật thông tin camera. Chấp nhận multipart/form-data. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> UpdateCamera(Guid id, [FromBody] CameraRequest cameraRequest)
		{
			var existingCamera = await _cameraService.GetByIdAsync(id);
			if (existingCamera == null)
			{
				return NotFound();
			}
			var cameraToUpdate = _autoMapper.Map<Camera>(cameraRequest);
			cameraToUpdate.Id = id;
			var result = await _cameraService.UpdateAsync(cameraToUpdate);
			return Ok(new { Message = "Cập nhật camera thành công." });
		}

		[HttpDelete("{id:guid}")]
		[SwaggerOperation(Summary = "Xóa camera", Description = "Xóa camera theo id. Quyền: Người dùng đã đăng nhập")]
		public async Task<IActionResult> DeleteCamera(Guid id)
		{
			var existingCamera = await _cameraService.GetByIdAsync(id);
			if (existingCamera == null)
			{
				return NotFound();
			}
			var result = await _cameraService.DeleteAsync(id);
			return NoContent();
		}

		// QR: Owner/Admin scan để xem thông tin + lịch sử camera
		[HttpGet("{id:guid}/qr-history")]
		[Authorize(Roles = "Owner,Admin")]
		[SwaggerOperation(Summary = "Thông tin camera cho QR scan", Description = "Owner/Admin quét QR code trên thân máy để xem thông tin chi tiết + lịch sử booking/inspection của camera.")]
		public async Task<IActionResult> GetCameraQrHistory(Guid id)
		{
			var history = await _cameraService.GetHistoryForQrAsync(id);
			return Ok(history);
		}

		// So sánh tối đa 3 camera
		[HttpGet("compare")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "So sánh tối đa 3 camera", Description = "Trả về thông tin chi tiết của tối đa 3 camera để client hiển thị bảng so sánh.")]
		public async Task<IActionResult> Compare([FromQuery] Guid[] ids)
		{
			if (ids == null || ids.Length == 0) return BadRequest("Cần cung cấp ít nhất 1 id.");
			if (ids.Length > 3) return BadRequest("Chỉ được so sánh tối đa 3 camera.");

			var all = await _cameraService.GetAllAsync();
			var list = all.Where(c => ids.Contains(c.Id)).ToList();
			return Ok(list);
		}
	}
}
