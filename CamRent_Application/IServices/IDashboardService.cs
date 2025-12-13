using CamRent_Application.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CamRent_Application.IServices
{
	public interface IDashboardService
	{
		Task<AdminDashboardDTO> GetAdminDashboardAsync(CancellationToken ct = default);
		Task<ManagerDashboardDTO> GetManagerDashboardAsync(Guid managerUserId, CancellationToken ct = default);
		Task<OwnerDashboardDTO> GetOwnerDashboardAsync(Guid ownerUserId, CancellationToken ct = default);
		Task<StaffDashboardDTO> GetStaffDashboardAsync(Guid staffUserId, CancellationToken ct = default);

		// Lịch làm việc chi tiết của một staff trong khoảng thời gian
		Task<IReadOnlyList<StaffScheduleItemDTO>> GetStaffScheduleAsync(Guid staffUserId, DateTime? from, DateTime? to, CancellationToken ct = default);

		// Workload của tất cả staff trong chi nhánh mà manager đang quản lý
		Task<StaffWorkloadSummaryDTO> GetStaffWorkloadForManagerAsync(Guid managerUserId, DateTime? from, DateTime? to, CancellationToken ct = default);
	}
}
