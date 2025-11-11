using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.CameraModel;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CamerasController : ControllerBase
	{
		private readonly ICameraService _cameraService;
		private readonly IMapper _autoMapper;
		public CamerasController( ICameraService cameraService, IMapper autoMapper)
		{
			_cameraService = cameraService;
			_autoMapper = autoMapper;
		}

		[HttpGet]
		[AllowAnonymous]
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
			// Simple sort: createdAt desc by default (if present)
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

		[HttpGet("{id:guid}")]
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
		public async Task<IActionResult> GetCamerasByOwnerId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var cameras = await _cameraService.GetCamerasByOwnerIdAsync(Guid.Parse(userId));
			return Ok(cameras);
		}

		[HttpPost]
		public async Task<IActionResult> CreateCamera([FromBody] CameraRequest cameraRequest)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var camera = _autoMapper.Map<Camera>(cameraRequest);
			var result = await _cameraService.CreateAsync(camera);
			camera.OwnerUserId = Guid.Parse(userId);
			camera.DepositPercent= 0.2m; // Default deposit percent
			camera.PlatformFeePercent= 0.2m; // Default platform fee percent
			camera.BaseDailyRate= 100000m; // Default base daily rate
			return Ok();
		}

		[HttpPut("{id:guid}")]
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
			return Ok();
		}

		[HttpDelete("{id:guid}")]
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
	}
}
