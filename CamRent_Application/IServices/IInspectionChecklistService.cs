using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.InspectionChecklistDTO;

namespace CamRent_Application.IServices
{
	public interface IInspectionChecklistService
	{
		Task<ChecklistTemplateResponse?> GetActiveTemplateAsync(ItemType itemType, InspectionType? inspectionType);
		Task<ChecklistTemplateResponse?> GetTemplateByIdAsync(Guid id);
		Task<List<ChecklistTemplateSummaryResponse>> ListTemplatesAsync(ItemType? itemType, InspectionType? inspectionType);

		Task<Guid> CreateTemplateAsync(UpsertChecklistTemplateRequest request, Guid adminId);
		Task<int> UpdateTemplateAsync(Guid id, UpsertChecklistTemplateRequest request, Guid adminId);
		Task<int> DeleteTemplateAsync(Guid id);
		Task<int> SetActiveAsync(Guid id, bool isActive, Guid adminId);
	}
}
