using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.VerificationModel;
using static CamRent_Application.DTOs.VerificationRequestDTO;

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
		[Authorize]
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
				return NotFound("No verification requests found for the user.");
			}
			return Ok(verifications);
		}
		[HttpPost]
		[Authorize(Policy = "Owner")]
		public async Task<IActionResult> Create([FromBody] CreateVerificationRequestDTO request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
			var result = await _verificationService.CreateVerificationAsync(request, Guid.Parse(userId));
			if (result > 0)
			{
				return Ok(new { success = "Create verification request successful" });
			}
			return BadRequest("Failed to create verification request.");
		}

		[HttpPut("assign_staff")]
		[Authorize(Policy = "Manager")]
		public async Task<IActionResult> AssignStaffToVerification(Guid verificationId, Guid staffId)
		{
			var result = await _verificationService.AssignStaffToVerification(staffId,verificationId);
			if (result > 0)
			{
				return Ok(new { success = "Assign staff to verification request successful" });
			}
			return BadRequest("Failed to assign staff to verification request.");
		}
	}
}
