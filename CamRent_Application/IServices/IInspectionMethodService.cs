using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.InspectionChecklistDTO;

namespace CamRent_Application.IServices
{
	public interface IInspectionMethodService
	{
		Task<List<InspectionMethodResponse>> ListAsync(bool includeInactive = false);
		Task<InspectionMethodResponse?> GetByIdAsync(Guid id);
		Task<Guid> CreateAsync(UpsertInspectionMethodRequest request, Guid adminId);
		Task<int> UpdateAsync(Guid id, UpsertInspectionMethodRequest request, Guid adminId);
		Task<int> DeleteAsync(Guid id);
	}
}

