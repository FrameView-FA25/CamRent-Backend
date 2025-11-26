using System;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IDashboardService
	{
		Task<DTOs.AdminDashboardDTO> GetAdminDashboardAsync(CancellationToken ct = default);
		Task<DTOs.ManagerDashboardDTO> GetManagerDashboardAsync(Guid managerUserId, CancellationToken ct = default);
		Task<DTOs.OwnerDashboardDTO> GetOwnerDashboardAsync(Guid ownerUserId, CancellationToken ct = default);
	}
}




