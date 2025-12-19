using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Common;
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

		public async Task RemoveMemberFromBranchAsync(Guid branchId, Guid userId, CancellationToken ct = default)
		{
			// ====== RULES (theo checklist ảnh) ======
			// 1) Booking active (PendingApproval/Confirmed/PickedUp/Returned/Overdue) -> không cho xoá
			var activeBookingStatuses = new[]
			{
				BookingStatus.Confirmed,
				BookingStatus.PickedUp,
				BookingStatus.Returned,
				BookingStatus.Overdue
			};

			var activeBookings = await _unitOfWork.Repository<Booking>()
				.ListAsync(b => b.StaffId == userId
							 && b.BranchId == branchId
							 && activeBookingStatuses.Contains(b.Status));
			if (activeBookings.Any())
				throw new AppException("Không thể xoá: staff đang xử lý booking active (Pending/Confirmed/PickedUp/Returned/Overdue). Vui lòng reassign booking trước.");

			// 2) Contract đang quản lý (Draft/PendingSignatures) do staff tạo -> cần reassign trước khi xoá
			var activeContractStatuses = new[] { ContractStatus.Draft, ContractStatus.PendingSignatures };
			var activeContracts = await _unitOfWork.Repository<Contract>()
				.ListAsync(c => c.BranchId == branchId
							 && c.CreatedByUserId == userId
							 && activeContractStatuses.Contains(c.Status));
			if (activeContracts.Any())
				throw new AppException("Không thể xoá: staff đang quản lý contract active (Draft/PendingSignatures). Vui lòng reassign contract trước.");

			// 3) Verification đang được gán cho staff và còn Pending
			var pendingVerifications = await _unitOfWork.Repository<VerificationRequest>()
				.ListAsync(v => v.BranchId == branchId
							 && v.StaffId == userId
							 && v.Status == VerificationStatus.Pending);
			if (pendingVerifications.Any())
				throw new AppException("Không thể xoá: staff đang xử lý verification Pending. Vui lòng reassign/hoàn thành verification trước.");

			// 4) Inspection đang thực hiện (Passed == null) do staff tạo
			var pendingInspections = await _unitOfWork.Repository<InspectionForm>()
				.ListAsync(f => f.BranchId == branchId
							 && f.CreatedByUserId == userId
							 && f.OverallPassed == null);
			if (pendingInspections.Any())
				throw new AppException("Không thể xoá: staff đang thực hiện inspection (chưa có kết quả). Vui lòng hoàn thành inspection trước.");

			// 5) Dispute đang xử lý: dispute open/under_review của booking trong chi nhánh mà staff phụ trách
			// (model Dispute không có StaffId nên suy luận theo Booking.StaffId)
			var disputeStatuses = new[] { "open", "under_review" };
			var staffBookingIds = (await _unitOfWork.Repository<Booking>()
					.ListAsync(b => b.BranchId == branchId && b.StaffId == userId))
				.Select(b => b.Id)
				.ToHashSet();

			if (staffBookingIds.Count > 0)
			{
				var disputes = await _unitOfWork.Repository<Dispute>()
					.ListAsync(d => staffBookingIds.Contains(d.BookingId)
								 && disputeStatuses.Contains(d.Status));
				if (disputes.Any())
					throw new AppException("Không thể xoá: staff đang xử lý dispute (open/under_review). Vui lòng reassign/hoàn thành dispute trước.");
			}

			// ====== DELETE MEMBERSHIP ======
			var membership = (await _unitOfWork.Repository<UserBranchMembership>()
				.ListAsync(m => m.BranchId == branchId && m.UserId == userId))
				.FirstOrDefault();

			if (membership == null)
				throw new AppException("Không tìm thấy thành viên trong chi nhánh này.");

			await _unitOfWork.Repository<UserBranchMembership>().DeleteAsync(membership.Id);
			await _unitOfWork.Complete();
		}

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
