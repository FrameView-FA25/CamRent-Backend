using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.InspectionModel;
using static CamRent_Application.DTOs.InspectionDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Consumes("multipart/form-data")]
	public class InspectionsController : ControllerBase
	{
		private readonly IInspectionService _inspectionService;
		private readonly IFileStorageService _fileStorageService;
		public InspectionsController(IInspectionService inspectionService, IFileStorageService fileStorageService)
		{
			_inspectionService = inspectionService;
			_fileStorageService = fileStorageService;
		}

		[HttpPost]
		[Authorize(Roles = "Staff")]
		public async Task<IActionResult> CreateInspection([FromForm] InspectionRequest inspectionRequestModel, List<IFormFile> files)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			// 1. Tạo inspection, lấy ra Id
			var inspectionId = await _inspectionService.CreateInspectionAsync(inspectionRequestModel);
			if(inspectionId == Guid.Empty)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create inspection.");
			}
			// 2. Nếu có file thì upload, ownerId = inspectionId
			if (files != null && files.Count > 0)
			{
				foreach (var file in files)
				{
					if (file == null || file.Length == 0) continue;

					await _fileStorageService.UploadAsync(
						file,
						ownerId: inspectionId,
						ownerType: FileOwnerType.Inspection,
						folder: $"camrent/inspections/{inspectionId}",
						label: $"{inspectionRequestModel.Type}-{inspectionRequestModel.Section}-{inspectionRequestModel.Label}"
					);
				}
			}

			return Ok(new{Message = "Inspection created successfully."});
		}

	}
}
