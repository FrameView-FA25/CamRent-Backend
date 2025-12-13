using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.WorkSlotDTO;

namespace CamRent_Application.IServices
{
	public interface IWorkSlotService
	{
		Task<List<WorkSlotResponse>> GetSlotsAsync(CancellationToken ct = default);
		Task<WorkSlotResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
		Task<Guid> CreateAsync(CreateWorkSlotRequest request, CancellationToken ct = default);
		Task<bool> UpdateAsync(Guid id, UpdateWorkSlotRequest request, CancellationToken ct = default);
		Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
	}
}


