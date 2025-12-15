using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.BranchDTO;

namespace CamRent_Application.IServices
{
	public interface IBranchService
	{
		Task<int> CreateBranchAsync(BranchRequest branchRequest);
		Task<int> AssignManagerToBranchAsync(Guid branchId, Guid managerId);
		Task<int> AssignStaffToBranchAsync(Guid branchId, Guid staffId);
		Task<List<BranchResponse>> GetAllBranchesAsync();
		Task<BranchResponse?> GetBranchByIdAsync(Guid branchId);
		Task<Guid> GetBranchIdByManagerIdAsync(Guid managerId);
		Task<List<BranchMembership>> GetBranchMembershipsAsync(Guid? branchId, Guid? managerId);
		Task<List<BranchMembership>> GetUnassignedStaffAsync();
		Task<List<BranchMembership>> GetUnassignedManagersAsync();
		Task RemoveMemberFromBranchAsync(Guid branchId, Guid userId, CancellationToken ct = default);
	}
}
