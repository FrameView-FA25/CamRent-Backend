using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Api.Models.CameraModel;
using static CamRent_Application.DTOs.CameraDTO;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	
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
		public async Task<IActionResult> GetAllCameras()
		{
			var cameras = await _cameraService.GetAllAsync();
			return Ok(cameras);
		}

		// Camera theo chi nhánh mà Manager quản lý
		[HttpGet("my-branch")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Danh sách camera của chi nhánh manager", Description = "Trả về danh sách camera thuộc chi nhánh mà BranchManager đang quản lý.")]
		public async Task<IActionResult> GetCamerasForMyBranch()
		{

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var managerId = Guid.Parse(userId);
			var cameras = await _cameraService.GetByBranchManagerAsync(managerId);
			return Ok(cameras);
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
		[Authorize(Policy = "Owner")]
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
		[Consumes("multipart/form-data")]
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
		public async Task<IActionResult> UpdateCamera(Guid id, [FromForm] UpdateCameraRequest model)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrEmpty(userId))
				return Unauthorized();
			var result = await _cameraService.UpdateAsync(model, Guid.Parse(userId));
			// 1) Lấy camera entity (đã include Media)
			var existingCamera = await _cameraService.GetByIdAsync(id);
			if (existingCamera == null)
				return NotFound();

			// 2) Check quyền
			if (existingCamera.OwnerUserId != Guid.Parse(userId))
				return Forbid();


			existingCamera.Media ??= new List<FileAssetDTO>();

			// 4) XÓA MEDIA CŨ
			if (model.RemoveMediaIds != null && model.RemoveMediaIds.Any())
			{
				var toRemove = existingCamera.Media
					.Where(m => model.RemoveMediaIds.Contains(m.Id))
					.ToList();

				foreach (var file in toRemove)
				{
					await _fileStorageService.DeleteByAssetIdAsync(file.Id);
					existingCamera.Media.Remove(file);
				}
			}

			// 5) THÊM MEDIA MỚI (giống Create)
			if (model.MediaFiles != null)
			{
				foreach (var file in model.MediaFiles)
				{
					if (file == null || file.Length <= 0) continue;

					var asset = await _fileStorageService.UploadAsync(
						file,
						ownerId: existingCamera.Id,
						ownerType: FileOwnerType.Camera,
						folder: $"camrent/cameras/{existingCamera.Id}",
						label: $"{existingCamera.Brand} {existingCamera.Model}"
					);
				}
			}

			if (result <= 0)
				return BadRequest(new { Message = "Cập nhật camera thất bại." });

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
