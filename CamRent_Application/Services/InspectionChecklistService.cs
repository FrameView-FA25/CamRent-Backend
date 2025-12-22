using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static CamRent_Application.DTOs.InspectionChecklistDTO;

namespace CamRent_Application.Services
{
	public class InspectionChecklistService : IInspectionChecklistService
	{
		private readonly IUnitOfWork _unitOfWork;

		public InspectionChecklistService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<ChecklistTemplateResponse?> GetActiveTemplateAsync(ItemType itemType, InspectionType? inspectionType)
		{
			var templates = (await _unitOfWork.Repository<InspectionChecklistTemplate>()
				.ListAsync(
					filter: t =>
						t.ItemType == itemType &&
						t.IsActive &&
						(inspectionType == null || t.InspectionType == inspectionType || t.InspectionType == null),
					orderBy: q => q
						.OrderByDescending(t => inspectionType != null && t.InspectionType == inspectionType)
						.ThenByDescending(t => t.UpdatedAt ?? t.CreatedAt),
					include: q => q
						.Include(t => t.Sections)
						.ThenInclude(s => s.Items)
						.ThenInclude(i => i.AllowedMethods)
						.ThenInclude(am => am.Method)
				)).ToList();

			var template = templates.FirstOrDefault();
			return template == null ? null : MapTemplate(template);
		}

		public async Task<ChecklistTemplateResponse?> GetTemplateByIdAsync(Guid id)
		{
			var templates = (await _unitOfWork.Repository<InspectionChecklistTemplate>()
				.ListAsync(
					filter: t => t.Id == id,
					include: q => q
						.Include(t => t.Sections)
						.ThenInclude(s => s.Items)
						.ThenInclude(i => i.AllowedMethods)
						.ThenInclude(am => am.Method)
				)).ToList();

			var template = templates.FirstOrDefault();
			return template == null ? null : MapTemplate(template);
		}

		public async Task<List<ChecklistTemplateSummaryResponse>> ListTemplatesAsync(ItemType? itemType, InspectionType? inspectionType)
		{
			var templates = (await _unitOfWork.Repository<InspectionChecklistTemplate>()
				.ListAsync(
					filter: t =>
						(itemType == null || t.ItemType == itemType) &&
						(inspectionType == null || t.InspectionType == inspectionType),
					orderBy: q => q.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
				)).ToList();

			return templates.Select(t => new ChecklistTemplateSummaryResponse
			{
				Id = t.Id,
				Name = t.Name,
				ItemType = t.ItemType,
				InspectionType = t.InspectionType,
				IsActive = t.IsActive,
				CreatedAt = t.CreatedAt,
				UpdatedAt = t.UpdatedAt
			}).ToList();
		}

		public async Task<Guid> CreateTemplateAsync(UpsertChecklistTemplateRequest request, Guid adminId)
		{
			await EnsureMethodsExistAsync(request.Sections.SelectMany(s => s.Items).SelectMany(i => i.AllowedMethodIds));

			var template = new InspectionChecklistTemplate
			{
				Name = request.Name.Trim(),
				ItemType = request.ItemType,
				InspectionType = request.InspectionType,
				IsActive = request.IsActive,
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = adminId
			};

			template.Sections = request.Sections
				.OrderBy(s => s.SortOrder)
				.Select(s => new InspectionChecklistSection
				{
					SortOrder = s.SortOrder,
					CreatedAt = DateTime.UtcNow,
					CreatedByUserId = adminId,
					Items = s.Items
						.OrderBy(i => i.SortOrder)
						.Select(i => new InspectionChecklistItem
						{
							Label = i.Label.Trim(),
							SortOrder = i.SortOrder,
							CreatedAt = DateTime.UtcNow,
							CreatedByUserId = adminId,
							AllowedMethods = i.AllowedMethodIds.Distinct().Select(methodId => new InspectionChecklistItemAllowedMethod
							{
								MethodId = methodId,
								CreatedAt = DateTime.UtcNow,
								CreatedByUserId = adminId
							}).ToList()
						}).ToList()
				}).ToList();

			if (template.IsActive)
			{
				await DeactivateOthersAsync(template.ItemType, template.InspectionType, excludeId: null);
			}

			await _unitOfWork.Repository<InspectionChecklistTemplate>().AddAsync(template);
			await _unitOfWork.Complete();
			return template.Id;
		}

		public async Task<int> UpdateTemplateAsync(Guid id, UpsertChecklistTemplateRequest request, Guid adminId)
		{
			var templates = (await _unitOfWork.Repository<InspectionChecklistTemplate>()
				.ListAsync(
					filter: t => t.Id == id,
					include: q => q
						.Include(t => t.Sections)
						.ThenInclude(s => s.Items)
						.ThenInclude(i => i.AllowedMethods)
				)).ToList();
			var template = templates.FirstOrDefault();
			if (template == null) return 0;

			// delete old children explicitly
			foreach (var section in template.Sections.ToList())
			{
				foreach (var item in section.Items.ToList())
				{
					foreach (var am in item.AllowedMethods.ToList())
					{
						await _unitOfWork.Repository<InspectionChecklistItemAllowedMethod>().DeleteAsync(am.Id);
					}
					await _unitOfWork.Repository<InspectionChecklistItem>().DeleteAsync(item.Id);
				}
				await _unitOfWork.Repository<InspectionChecklistSection>().DeleteAsync(section.Id);
			}

			await EnsureMethodsExistAsync(request.Sections.SelectMany(s => s.Items).SelectMany(i => i.AllowedMethodIds));

			template.Name = request.Name.Trim();
			template.ItemType = request.ItemType;
			template.InspectionType = request.InspectionType;
			template.IsActive = request.IsActive;
			template.UpdatedAt = DateTime.UtcNow;
			template.UpdatedByUserId = adminId;

			template.Sections = request.Sections
				.OrderBy(s => s.SortOrder)
				.Select(s => new InspectionChecklistSection
				{
					TemplateId = template.Id,
					SortOrder = s.SortOrder,
					CreatedAt = DateTime.UtcNow,
					CreatedByUserId = adminId,
					Items = s.Items
						.OrderBy(i => i.SortOrder)
						.Select(i => new InspectionChecklistItem
						{
							Label = i.Label.Trim(),
							SortOrder = i.SortOrder,
							CreatedAt = DateTime.UtcNow,
							CreatedByUserId = adminId,
							AllowedMethods = i.AllowedMethodIds.Distinct().Select(methodId => new InspectionChecklistItemAllowedMethod
							{
								MethodId = methodId,
								CreatedAt = DateTime.UtcNow,
								CreatedByUserId = adminId
							}).ToList()
						}).ToList()
				}).ToList();

			if (template.IsActive)
			{
				await DeactivateOthersAsync(template.ItemType, template.InspectionType, excludeId: template.Id);
			}

			await _unitOfWork.Repository<InspectionChecklistTemplate>().UpdateAsync(template);
			return await _unitOfWork.Complete();
		}

		public async Task<int> DeleteTemplateAsync(Guid id)
		{
			var templates = (await _unitOfWork.Repository<InspectionChecklistTemplate>()
				.ListAsync(
					filter: t => t.Id == id,
					include: q => q
						.Include(t => t.Sections)
						.ThenInclude(s => s.Items)
						.ThenInclude(i => i.AllowedMethods)
				)).ToList();
			var template = templates.FirstOrDefault();
			if (template == null) return 0;

			foreach (var section in template.Sections.ToList())
			{
				foreach (var item in section.Items.ToList())
				{
					foreach (var am in item.AllowedMethods.ToList())
					{
						await _unitOfWork.Repository<InspectionChecklistItemAllowedMethod>().DeleteAsync(am.Id);
					}
					await _unitOfWork.Repository<InspectionChecklistItem>().DeleteAsync(item.Id);
				}
				await _unitOfWork.Repository<InspectionChecklistSection>().DeleteAsync(section.Id);
			}

			await _unitOfWork.Repository<InspectionChecklistTemplate>().DeleteAsync(id);
			return await _unitOfWork.Complete();
		}

		public async Task<int> SetActiveAsync(Guid id, bool isActive, Guid adminId)
		{
			var template = await _unitOfWork.Repository<InspectionChecklistTemplate>().GetByIdAsync(id);
			if (template == null) return 0;

			template.IsActive = isActive;
			template.UpdatedAt = DateTime.UtcNow;
			template.UpdatedByUserId = adminId;

			if (isActive)
			{
				await DeactivateOthersAsync(template.ItemType, template.InspectionType, excludeId: template.Id);
			}

			await _unitOfWork.Repository<InspectionChecklistTemplate>().UpdateAsync(template);
			return await _unitOfWork.Complete();
		}

		private async Task DeactivateOthersAsync(ItemType itemType, InspectionType? inspectionType, Guid? excludeId)
		{
			var others = (await _unitOfWork.Repository<InspectionChecklistTemplate>()
				.ListAsync(t =>
					t.ItemType == itemType &&
					t.InspectionType == inspectionType &&
					t.IsActive &&
					(excludeId == null || t.Id != excludeId.Value)))
				.ToList();

			if (others.Count == 0) return;

			foreach (var t in others)
			{
				t.IsActive = false;
				await _unitOfWork.Repository<InspectionChecklistTemplate>().UpdateAsync(t);
			}

			await _unitOfWork.Complete();
		}

		private static ChecklistTemplateResponse MapTemplate(InspectionChecklistTemplate template)
		{
			return new ChecklistTemplateResponse
			{
				Id = template.Id,
				Name = template.Name,
				ItemType = template.ItemType,
				InspectionType = template.InspectionType,
				IsActive = template.IsActive,
				Sections = template.Sections
					.OrderBy(s => s.SortOrder)
					.Select(s => new ChecklistSectionResponse
					{
						Id = s.Id,
						SortOrder = s.SortOrder,
						Items = s.Items
							.OrderBy(i => i.SortOrder)
							.Select(i => new ChecklistItemResponse
							{
								Id = i.Id,
								Label = i.Label,
								SortOrder = i.SortOrder,
								AllowedMethods = i.AllowedMethods
									.Select(am => am.Method)
									.Where(m => m != null && m.IsActive)
									.OrderBy(m => m.SortOrder)
									.Select(m => new InspectionMethodResponse
									{
										Id = m.Id,
										Code = m.Code,
										Name = m.Name,
										SortOrder = m.SortOrder,
										IsActive = m.IsActive
									}).ToList()
							}).ToList()
					}).ToList()
			};
		}

		private async Task EnsureMethodsExistAsync(IEnumerable<Guid> methodIds)
		{
			var ids = methodIds.Distinct().ToList();
			if (ids.Count == 0) return;

			var methods = (await _unitOfWork.Repository<InspectionMethod>()
				.ListAsync(m => ids.Contains(m.Id))).ToList();

			if (methods.Count != ids.Count)
				throw new InvalidOperationException("One or more inspection methods do not exist.");
		}
	}
}

