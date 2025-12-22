using CamRent_Application.DTOs;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static CamRent_Application.DTOs.InspectionDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/inspections")]
	public class InspectionsController : ControllerBase
	{
		private readonly IInspectionService _inspectionService;
		private readonly IFileStorageService _fileStorageService;

		public InspectionsController(IInspectionService inspectionService, IFileStorageService fileStorageService)
		{
			_inspectionService = inspectionService;
			_fileStorageService = fileStorageService;
		}

		[HttpGet("{id:guid}")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Lấy chi tiết 1 dòng inspection", Description = "Dùng để xem chi tiết 1 dòng checklist (row). Xem theo phiếu dùng /api/inspection-forms/{id}.")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var inspection = await _inspectionService.GetByIdAsync(id);
			if (inspection == null)
				return NotFound(new { Message = "Không tìm thấy inspection." });

			return Ok(inspection);
		}

		[HttpPut("{id:guid}")]
		[Authorize(Policy = "ManagerOrStaff")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Cập nhật media của 1 dòng inspection", Description = "Chỉ dùng để upload/xóa ảnh cho 1 dòng checklist. Update nội dung checklist dùng API inspection-forms.")]
		public async Task<IActionResult> UpdateMedia(Guid id, [FromForm] UpdateInspectionMediaRequest request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var existing = await _inspectionService.GetByIdAsync(id);
			if (existing == null)
				return NotFound();

			existing.Media ??= new List<FileAssetDTO>();

			if (request.RemoveMediaIds != null && request.RemoveMediaIds.Any())
			{
				var toRemove = existing.Media
					.Where(m => request.RemoveMediaIds.Contains(m.Id))
					.ToList();

				foreach (var file in toRemove)
				{
					await _fileStorageService.DeleteByAssetIdAsync(file.Id);
				}
			}

			if (request.Files != null)
			{
				foreach (var file in request.Files)
				{
					if (file == null || file.Length <= 0) continue;

					await _fileStorageService.UploadAsync(
						file,
						ownerId: existing.Id,
						ownerType: FileOwnerType.Inspection,
						folder: $"camrent/inspections/{id}",
						label: $"row-{existing.Section}-{existing.Label}"
					);
				}
			}

			return Ok(new { Message = "Updated media." });
		}
	}
}

