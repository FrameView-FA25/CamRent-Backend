using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
		public async Task<IActionResult> GetAllCameras()
		{
			var cameras = await _cameraService.GetAllAsync();
			return Ok(cameras);
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

		[HttpPost]
		public async Task<IActionResult> CreateCamera([FromBody] CameraRequest cameraRequest)
		{
			var camera = _autoMapper.Map<Camera>(cameraRequest);
			var result = await _cameraService.CreateAsync(camera);
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
