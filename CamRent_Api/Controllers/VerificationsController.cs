using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.VerificationModel;
using static CamRent_Application.DTOs.VerificationRequestDTO;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class VerificationsController : ControllerBase
	{
		private readonly IVerificationService _verificationService;
		public VerificationsController(IVerificationService verificationService)
		{
			_verificationService = verificationService;
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
			if (result > 0)
			{
				return Ok(new { Message = "Tạo yêu cầu xác minh thành công." });
			}
			return BadRequest(new { Message = "Tạo yêu cầu xác minh thất bại." });
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
	}
}
