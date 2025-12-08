using CamRent_Application.IServices;
using CamRent_Application.Services;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using static CamRent_Api.Models.ContractModel;
using static CamRent_Api.Models.VerificationModel;
using static CamRent_Application.DTOs.VerificationRequestDTO;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class VerificationsController : ControllerBase
	{
		private readonly IVerificationService _verificationService;
		private readonly IContractService _contractService;
		public VerificationsController(IVerificationService verificationService, IContractService contractService)
		{
			_verificationService = verificationService;
			_contractService = contractService;
		}
		[HttpGet("get_by_user_id")]
		[Authorize(Policy ="OwnerOrManagerOrStaff")]
		[SwaggerOperation(Summary = "Lấy verification theo người dùng", Description = "Trả về các yêu cầu verification mà người dùng hiện tại có quyền xem, phụ thuộc vào vai trò. Quyền: Owner, BranchManager, Staff, Admin")]
		public async Task<IActionResult> GetAllByUserId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var role = User.FindAll(ClaimTypes.Role).Select(r => r.Value).FirstOrDefault();
			var verifications = new List<VerificationResponseDTO>();
			if (role == UserRole.Staff.ToString())
			{
				verifications = await _verificationService.GetVerificationByStaffId(Guid.Parse(userId));
			}
			else if (role == UserRole.BranchManager.ToString())
			{
				verifications = await _verificationService.GetVerificationByManagerId(Guid.Parse(userId));
			}
			else if( role == UserRole.Owner.ToString())
			{
				verifications = await _verificationService.GetVerificationByOwnerId(Guid.Parse(userId));
			}
			if (verifications == null || verifications.Count == 0)
			{
				return NotFound("Không tìm thấy yêu cầu xác minh nào cho người dùng.");
			}
			return Ok(verifications);
		}
		[HttpPost]
		[Authorize(Policy = "Owner")]
		[SwaggerOperation(Summary = "Tạo yêu cầu xác minh", Description = "Tạo một yêu cầu xác minh thay mặt chủ sở hữu. Quyền: Owner, Admin")]
		public async Task<IActionResult> Create([FromBody] CreateVerificationRequestDTO request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var result = await _verificationService.CreateVerificationAsync(request, Guid.Parse(userId));
			if (result == Guid.Empty)
			{
				return Ok(new { Message = "Tạo yêu cầu xác minh thất bại." });
			}
			var contract = await _contractService.CreateVerificationContractAsync(result, Guid.Parse(userId));

			var response = new CreateContractResponse
			{
				ContractId = contract.Id
			};
			return Ok(response);
		}

		[HttpPut("assign_staff")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Gán nhân viên cho yêu cầu xác minh", Description = "Gán một nhân viên phụ trách cho yêu cầu xác minh. Quyền: BranchManager, Admin")]
		public async Task<IActionResult> AssignStaffToVerification(Guid verificationId, Guid staffId)
		{
			var result = await _verificationService.AssignStaffToVerification(staffId, verificationId);
			if (result > 0)
			{
				return Ok(new { Message = "Gán nhân viên thành công." });
			}
			return BadRequest(new { Message = "Gán nhân viên thất bại." });
		}

		// New: get detail by id
		[HttpGet("{id}")]
		[Authorize(Policy = "OwnerOrManagerOrStaff")]
		[SwaggerOperation(Summary = "Lấy chi tiết verification theo id", Description = "Trả về chi tiết của một verification theo id.")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var verification = await _verificationService.GetVerificationById(id);
			if (verification == null)
			{
				return NotFound(new { Message = "Không tìm thấy yêu cầu xác minh." });
			}
			return Ok(verification);
		}

		// New: update verification
		[HttpPut("{id}")]
		[Authorize(Policy = "Owner")]
		[SwaggerOperation(Summary = "Cập nhật verification", Description = "Cập nhật thông tin một yêu cầu verification. Quyền: Owner.")]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVerificationRequestDTO request)
		{
			var result = await _verificationService.UpdateVerificationAsync(id, request);
			if (result > 0)
			{
				return Ok(new { Message = "Cập nhật thành công." });
			}
			return BadRequest(new { Message = "Cập nhật thất bại hoặc không tìm thấy yêu cầu." });
		}

		[HttpPut("{id}/update-status")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Cập nhật trạng thái verification", Description = "Cập nhật trạng thái của một yêu cầu verification. Quyền: BranchManager.")]
		public async Task<IActionResult> UpdateStatus(Guid id, string note, VerificationStatus status)
		{
			var result = await _verificationService.UpdateVerificationStatusAsync(id, note, status);
			if (result > 0)
			{
				return Ok(new { Message = "Cập nhật trạng thái thành công." });
			}
			return BadRequest(new { Message = "Cập nhật trạng thái thất bại hoặc không tìm thấy yêu cầu." });
		}

		// New: delete verification
		[HttpDelete("{id}")]
		[Authorize(Policy = "Owner")]
		[SwaggerOperation(Summary = "Xóa verification", Description = "Xóa một yêu cầu verification theo id. Quyền: Owner")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var result = await _verificationService.DeleteVerificationAsync(id);
			if (result > 0)
			{
				return Ok(new { Message = "Xóa thành công." });
			}
			return BadRequest(new { Message = "Xóa thất bại hoặc không tìm thấy yêu cầu." });
		}

		
	}
}
