using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Application.DTOs.BranchDTO;

namespace CamRent_Api.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class BranchsController : ControllerBase
	{
		private readonly IBranchService _branchService;
		public BranchsController(IBranchService branchService)
		{
			_branchService = branchService;
		}
		[HttpGet]
		public async Task<IActionResult> GetAllBranches()
		{
			var branches = await _branchService.GetAllBranchesAsync();
			return Ok(branches);
		}
		[HttpGet("/memberships")]
		public async Task<IActionResult> GetBranchMemberships(Guid? branchId)
		{
			var userId = string.Empty;
			var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
			foreach (var role in roles)
			{
				if (role == UserRole.BranchManager.ToString())
				{
					userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;
				}
			}
			Guid? managerId = Guid.Parse(userId!);
			var memberships = await _branchService.GetBranchMembershipsAsync(branchId, managerId);
			return Ok(memberships);
		}
		[HttpGet("{id:guid}")]
		public async Task<IActionResult> GetBranchById(Guid id)
		{
			var branch = await _branchService.GetBranchByIdAsync(id);
			if (branch == null)
			{
				return NotFound();
			}
			return Ok(branch);
		}
		[HttpPost]
		public async Task<IActionResult> CreateBranch([FromBody] BranchRequest branchRequest)
		{
			var result = await _branchService.CreateBranchAsync(branchRequest);
			return result > 0 ? Ok() : BadRequest();
		}

		[HttpPut("{branchId:guid}/assign-manager/{managerId:guid}")]
		public async Task<IActionResult> AssignManagerToBranch(Guid branchId, Guid managerId)
		{
			var result = await _branchService.AssignManagerToBranchAsync(branchId, managerId);
			return result > 0 ? Ok() : BadRequest();
		}

	}
}
