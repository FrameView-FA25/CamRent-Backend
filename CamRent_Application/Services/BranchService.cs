using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.BranchDTO;

namespace CamRent_Application.Services
{
	public class BranchService : IBranchService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		public BranchService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}
		public async Task<int> AssignManagerToBranchAsync(Guid branchId, Guid managerId)
		{
			var branch = await _unitOfWork.Repository<Branch>().GetByIdAsync(branchId);
			if (branch == null)
			{
				throw new Exception("Branch not found");
			}
			branch.ManagerId = managerId;
			await _unitOfWork.Repository<Branch>().UpdateAsync(branch);
			var membership = new UserBranchMembership
			{
				BranchId = branchId,
				UserId = managerId
			};
			await _unitOfWork.Repository<UserBranchMembership>().AddAsync(membership);
			return await _unitOfWork.Complete();
		}

		public async Task<int> AssignStaffToBranchAsync(Guid branchId, Guid staffId)
		{
			var branch = await _unitOfWork.Repository<Branch>().GetByIdAsync(branchId);
			if (branch == null)
			{
				throw new Exception("Branch not found");
			}
			var membership = new UserBranchMembership
			{
				BranchId = branchId,
				UserId = staffId
			};
			await _unitOfWork.Repository<UserBranchMembership>().AddAsync(membership);
			return await _unitOfWork.Complete();
		}

		public async Task<int> CreateBranchAsync(BranchRequest branchRequest)
		{
			var branch = _mapper.Map<Branch>(branchRequest);
			await _unitOfWork.Repository<Branch>().AddAsync(branch);
			return await _unitOfWork.Complete();
		}

		public async Task<List<BranchResponse>> GetAllBranchesAsync()
		{
			var branches = await _unitOfWork.Repository<Branch>().ListAsync(include: b => b.Include(b => b.Manager).Include(b => b.UserMemberships));
			return _mapper.Map<List<BranchResponse>>(branches);

		}

		public async Task<BranchResponse?> GetBranchByIdAsync(Guid branchId)
		{
			var branch = (await _unitOfWork.Repository<Branch>()
				.ListAsync(filter: b => b.Id == branchId, include: b => b.Include(b => b.Manager).Include(b => b.UserMemberships))).FirstOrDefault();
			var branchResponse = _mapper.Map<BranchResponse>(branch);
			return branchResponse;
		}

		public async Task<Guid> GetBranchIdByManagerIdAsync(Guid managerId)
		{
			var branch = (await _unitOfWork.Repository<Branch>()
				.ListAsync(filter: b => b.ManagerId == managerId))
				.FirstOrDefault();
			if (branch is null)
				throw new KeyNotFoundException($"No branch found for manager {managerId}.");
			return branch.Id;
		}

		public async Task<List<BranchMembership>> GetBranchMembershipsAsync(Guid? branchId,Guid? managerId)
		{
			if (branchId is null && managerId is null)
				throw new ArgumentException("Provide either branchId or managerId.");

			Guid resolvedBranchId;

			if (branchId is not null)
			{
				var branch = await _unitOfWork.Repository<Branch>().GetByIdAsync(branchId.Value);
				if (branch is null)
					throw new KeyNotFoundException($"Branch {branchId} not found.");
				resolvedBranchId = branch.Id;
			}
			else
			{
				var branch = (await _unitOfWork.Repository<Branch>()
					.ListAsync(filter: b => b.ManagerId == managerId))
					.FirstOrDefault();

				if (branch is null)
					throw new KeyNotFoundException($"No branch found for manager {managerId}.");
				resolvedBranchId = branch.Id;
			}

			var memberships = await _unitOfWork.Repository<UserBranchMembership>()
				.ListAsync(
					filter: ub => ub.BranchId == resolvedBranchId && ub.UserId != managerId,
					include: ub => ub.Include(x => x.User));

			return _mapper.Map<List<BranchMembership>>(memberships);
		}

		public Task<List<BranchMembership>> GetUnassignedStaffAsync()
			=> GetUsersByRoleWithoutBranchAsync(UserRole.Staff);

		public Task<List<BranchMembership>> GetUnassignedManagersAsync()
			=> GetUsersByRoleWithoutBranchAsync(UserRole.BranchManager);

		private async Task<List<BranchMembership>> GetUsersByRoleWithoutBranchAsync(UserRole role)
		{
			// 1) Users có role tương ứng
			var roleMappings = await _unitOfWork.Repository<UserRoleMapping>()
				.ListAsync(rm => rm.Role == role);
			var roleUserIds = roleMappings.Select(x => x.UserId).Distinct().ToHashSet();

			if (roleUserIds.Count == 0)
				return new List<BranchMembership>();

			// 2) Users đã thuộc ít nhất 1 chi nhánh
			var memberships = await _unitOfWork.Repository<UserBranchMembership>().ListAsync();
			var assignedUserIds = memberships.Select(m => m.UserId).Distinct().ToHashSet();

			// Với BranchManager: nếu đã được gán làm ManagerId của bất kỳ Branch nào thì coi như "đã thuộc chi nhánh"
			// (tránh trường hợp dữ liệu thiếu UserBranchMembership nhưng Branch.ManagerId đã set).
			if (role == UserRole.BranchManager)
			{
				var branches = await _unitOfWork.Repository<Branch>().ListAsync(b => b.ManagerId != null);
				foreach (var b in branches)
				{
					if (b.ManagerId.HasValue)
						assignedUserIds.Add(b.ManagerId.Value);
				}
			}

			// 3) Lọc userId chưa có membership
			var unassignedIds = roleUserIds.Except(assignedUserIds).ToList();
			if (unassignedIds.Count == 0)
				return new List<BranchMembership>();

			var users = await _unitOfWork.Repository<User>()
				.ListAsync(u => unassignedIds.Contains(u.Id));

			return users
				.OrderBy(u => u.FullName)
				.Select(u => new BranchMembership
				{
					UserId = u.Id,
					FullName = u.FullName,
					Phone = u.Phone,
					Email = u.Email
				})
				.ToList();
		}
	}
}
