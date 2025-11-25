using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Api.Models.InspectionModel;
using static CamRent_Application.DTOs.InspectionDTO;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	
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
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Tạo inspection", Description = "Tạo một inspection và tải lên các file liên quan. Các file tải lên sẽ được gắn với inspection vừa tạo. Quyền: Staff")]
		public async Task<IActionResult> CreateInspection([FromForm] InspectionRequest inspectionRequest)
		{
			// Lấy userId từ token
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			// 1. Tạo inspection, lấy ra Id
			var inspectionId = await _inspectionService.CreateInspectionAsync(inspectionRequest, Guid.Parse(userId));
			if(inspectionId == Guid.Empty)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "Tạo inspection thất bại.");
			}
			// 2. Nếu có file thì upload, ownerId = inspectionId
			if (inspectionRequest.Files != null && inspectionRequest.Files.Count > 0)
			{
				foreach (var file in inspectionRequest.Files)
				{
					if (file == null || file.Length == 0) continue;

					await _fileStorageService.UploadAsync(
						file,
						ownerId: inspectionId,
						ownerType: FileOwnerType.Inspection,
						folder: $"camrent/inspections/{inspectionId}",
						label: $"{inspectionRequest.Type}-{inspectionRequest.Section}-{inspectionRequest.Label}"
					);
				}
			}

			return Ok(new{Message = "Tạo inspection thành công."});
		}

		[HttpGet("booking/{bookingId:guid}")]
		[Authorize(Policy ="Staff")]
		[SwaggerOperation(Summary = "Inspection của booking", Description = "Trả về danh sách inspection cho một booking.")]
		public async Task<ActionResult<IEnumerable<InspectionResponseDTO>>> GetByBookingId(Guid bookingId)
		{
			var inspections = await _inspectionService.GetByBookingAsync(bookingId);
			return Ok(inspections);
		}

		// New: inspections attached to a verification request
		[HttpGet("verification/{verificationId:guid}")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Inspections for a verification request", Description = "Trả về danh sách inspection gắn với một VerificationRequest.")]
		public async Task<ActionResult<IEnumerable<InspectionResponseDTO>>> GetByVerificationId(Guid verificationId)
		{
			var inspections = await _inspectionService.GetByVerificationAsync(verificationId);
			return Ok(inspections);
		}

		// New: get detail by id
		[HttpGet("{id:guid}")]
		[Authorize(Policy = "Staff")]
		[SwaggerOperation(Summary = "Lấy chi tiết inspection theo id", Description = "Trả về chi tiết của một inspection theo id.")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var inspection = await _inspectionService.GetByIdAsync(id);
			if (inspection == null)
				return NotFound(new { Message = "Không tìm thấy inspection." });

			return Ok(inspection);
		}

		// New: update inspection
		[HttpPut("{id:guid}")]
		[Authorize(Policy = "ManagerOrStaff")]
		[Consumes("multipart/form-data")]
		[SwaggerOperation(Summary = "Cập nhật inspection", Description = "Cập nhật thông tin một inspection. Quyền: Staff.")]
		public async Task<IActionResult> Update(Guid id, [FromForm] InspectionRequest inspectionRequest)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _inspectionService.UpdateInspectionAsync(id, inspectionRequest, Guid.Parse(userId));
			if (result > 0)
			{
				// If there are uploaded files in the request, upload them and attach to the inspection
				if (inspectionRequest.Files != null && inspectionRequest.Files.Count > 0)
				{
					foreach (var file in inspectionRequest.Files)
					{
						if (file == null || file.Length == 0) continue;

						await _fileStorageService.UploadAsync(
							file,
							ownerId: id,
							ownerType: FileOwnerType.Inspection,
							folder: $"camrent/inspections/{id}",
							label: $"{inspectionRequest.Type}-{inspectionRequest.Section}-{inspectionRequest.Label}"
						);
					}
				}

				return Ok(new { Message = "Cập nhật inspection thành công." });
			}

			return BadRequest(new { Message = "Cập nhật thất bại hoặc không tìm thấy inspection." });
		}

		[HttpPut("{id:guid}/approve")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Phê duyệt inspection", Description = "Phê duyệt một inspection. Quyền: BranchManager.")]
		public async Task<IActionResult> ApproveInspection(Guid id, bool pass)
		{
			var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var result = await _inspectionService.ApproveInspectionAsync(id, Guid.Parse(managerId), pass);
			if (result > 0)
				return Ok(new { Message = "Inspection được phê duyệt thành công." });
			return BadRequest(new { Message = "Inspection phê duyệt thất bại." });
		}

		// New: delete inspection
		[HttpDelete("{id:guid}")]
		[Authorize(Policy = "ManagerOrStaff")]
		[SwaggerOperation(Summary = "Xóa inspection", Description = "Xóa một inspection theo id. Quyền: Staff.")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var result = await _inspectionService.DeleteInspectionAsync(id);
			if (result > 0)
				return Ok(new { Message = "Xóa inspection thành công." });

			return BadRequest(new { Message = "Xóa thất bại hoặc không tìm thấy inspection." });
		}
	}
}
