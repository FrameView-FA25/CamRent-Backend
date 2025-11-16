using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.InspectionModel;
using static CamRent_Application.DTOs.InspectionDTO;
using Swashbuckle.AspNetCore.Annotations;

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
		[SwaggerOperation(Summary = "Tạo inspection", Description = "Tạo một inspection và tải lên các file liên quan. Các file tải lên sẽ được gắn với inspection vừa tạo. Quyền: Staff")]
		public async Task<IActionResult> CreateInspection([FromForm] InspectionRequest inspectionRequestModel, List<IFormFile> files)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			// 1. Tạo inspection, lấy ra Id
			var inspectionId = await _inspectionService.CreateInspectionAsync(inspectionRequestModel);
			if(inspectionId == Guid.Empty)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "Tạo inspection thất bại.");
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

			return Ok(new{Message = "Tạo inspection thành công."});
		}

		// Biên lai inspection cho một booking (nhận/trả máy)
		[HttpGet("booking/{bookingId:guid}/receipts")]
		[Authorize]
		[SwaggerOperation(Summary = "Biên lai inspection của booking", Description = "Trả về danh sách inspection (nhận/trả máy) cho một booking. FE có thể dùng Label để hiển thị 'Nhận máy ảnh' / 'Đã trả máy ảnh'.")]
		public async Task<ActionResult<IEnumerable<InspectionResponseDTO>>> GetBookingReceipts(Guid bookingId)
		{
			var inspections = await _inspectionService.GetByBookingAsync(bookingId);
			return Ok(inspections);
		}

	}
}
