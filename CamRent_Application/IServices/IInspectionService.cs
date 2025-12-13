using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.InspectionDTO;

namespace CamRent_Application.IServices
{
	public interface IInspectionService
	{
		Task<Guid> CreateInspectionAsync(InspectionRequest inspectionRequest, Guid staffId);
		Task<List<InspectionResponseDTO>> GetByBookingAsync(Guid bookingId);
		Task<List<InspectionResponseDTO>> GetByVerificationAsync(Guid verificationId);
		Task<InspectionResponseDTO?> GetByIdAsync(Guid id);
		Task<int> UpdateInspectionAsync(Guid id, UpdateInspectionRequest request, Guid staffId);
		Task<int> DeleteInspectionAsync(Guid id);
	}
}
