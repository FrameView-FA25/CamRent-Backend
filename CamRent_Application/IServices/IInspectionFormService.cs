using System;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.InspectionFormDTO;

namespace CamRent_Application.IServices
{
	public interface IInspectionFormService
	{
		Task<Guid> CreateAsync(CreateInspectionFormRequest request, Guid staffId);
		Task<InspectionFormResponse?> GetByIdAsync(Guid formId);
		Task<List<InspectionFormSummaryResponse>> ListByBookingAsync(Guid bookingId);
		Task<List<InspectionFormSummaryResponse>> ListByVerificationAsync(Guid verificationId);
		Task<int> UpdateAsync(Guid formId, UpdateInspectionFormRequest request, Guid staffId);
	}
}
