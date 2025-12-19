using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static CamRent_Application.DTOs.InspectionChecklistDTO;
using static CamRent_Application.DTOs.InspectionFormDTO;

namespace CamRent_Application.Services
{
	public class InspectionFormService : IInspectionFormService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IInspectionChecklistService _checklistService;

		public InspectionFormService(IUnitOfWork unitOfWork, IInspectionChecklistService checklistService)
		{
			_unitOfWork = unitOfWork;
			_checklistService = checklistService;
		}

		public async Task<Guid> CreateAsync(CreateInspectionFormRequest request, Guid staffId)
		{
			// validate booking/verification existence
			if (request.Type == InspectionType.Booking)
			{
				var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(request.InspectionTypeId);
				if (booking == null) throw new InvalidOperationException("Booking not found for the given InspectionTypeId.");
			}
			else if (request.Type == InspectionType.Verification)
			{
				var verification = await _unitOfWork.Repository<VerificationRequest>().GetByIdAsync(request.InspectionTypeId);
				if (verification == null) throw new InvalidOperationException("VerificationRequest not found for the given InspectionTypeId.");
			}

			var template = await _checklistService.GetActiveTemplateAsync(request.ItemType, request.Type);
			if (template == null) throw new InvalidOperationException("No active checklist template found for this item type.");

			static string Key(string section, string label)
				=> $"{(section ?? string.Empty).Trim().ToLowerInvariant()}|{(label ?? string.Empty).Trim().ToLowerInvariant()}";

			var allowed = template.Sections
				.SelectMany(s => s.Items.Select(i => new
				{
					Key = Key(s.Name, i.Label),
					AllowedMethodIds = i.AllowedMethods.Select(m => m.Id).ToHashSet()
				}))
				.ToDictionary(x => x.Key, x => x.AllowedMethodIds);

			foreach (var row in request.Rows)
			{
				var rowKey = Key(row.Section, row.Label);
				if (!allowed.ContainsKey(rowKey))
					throw new InvalidOperationException($"Checklist row is not part of the active template: [{row.Section}] {row.Label}");

				var allowedMethodIds = allowed[rowKey];
				foreach (var methodId in row.MethodIds.Distinct())
				{
					if (!allowedMethodIds.Contains(methodId))
						throw new InvalidOperationException($"Method is not allowed for checklist row: [{row.Section}] {row.Label}");
				}
			}

			await EnsureMethodsExistAsync(request.Rows.SelectMany(r => r.MethodIds));

			var overallPassed = request.Passed ?? DeriveOverallPassed(request.Rows);
			if (overallPassed != null)
			{
				await UpdateItemConfirmationAsync(request.ItemType, request.ItemId, overallPassed.Value);
			}

			var form = new InspectionForm
			{
				Id = Guid.NewGuid(),
				TemplateId = template.Id,
				ItemType = request.ItemType,
				ItemId = request.ItemId,
				Type = request.Type,
				HandoverType = request.HandoverType,
				InspectionTypeId = request.InspectionTypeId,
				BranchId = request.BranchId,
				OverallPassed = overallPassed,
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = staffId
			};

			await _unitOfWork.Repository<InspectionForm>().AddAsync(form);

			var methodSelections = new List<InspectionMethodSelection>();
			foreach (var row in request.Rows)
			{
				var inspection = new Inspection
				{
					Id = Guid.NewGuid(),
					FormId = form.Id,
					CreatedAt = DateTime.UtcNow,
					CreatedByUserId = staffId,
					Section = row.Section.Trim(),
					Label = row.Label.Trim(),
					Value = null,
					Passed = row.Passed,
					Notes = row.Notes ?? string.Empty
				};

				await _unitOfWork.Repository<Inspection>().AddAsync(inspection);

				foreach (var methodId in row.MethodIds.Distinct())
				{
					methodSelections.Add(new InspectionMethodSelection
					{
						Id = Guid.NewGuid(),
						InspectionId = inspection.Id,
						MethodId = methodId,
						CreatedAt = DateTime.UtcNow,
						CreatedByUserId = staffId
					});
				}
			}

			foreach (var ms in methodSelections)
			{
				await _unitOfWork.Repository<InspectionMethodSelection>().AddAsync(ms);
			}

			await _unitOfWork.Complete();
			return form.Id;
		}

		public async Task<InspectionFormResponse?> GetByIdAsync(Guid formId)
		{
			var forms = (await _unitOfWork.Repository<InspectionForm>()
				.ListAsync(
					filter: f => f.Id == formId,
					include: q => q.Include(f => f.Template).Include(f => f.Staff)
				)).ToList();

			var form = forms.FirstOrDefault();
			if (form == null) return null;

			var inspections = (await _unitOfWork.Repository<Inspection>()
				.ListAsync(i => i.FormId == formId)).ToList();

			var inspectionIds = inspections.Select(i => i.Id).ToList();
			var selections = (await _unitOfWork.Repository<InspectionMethodSelection>()
				.ListAsync(
					filter: s => inspectionIds.Contains(s.InspectionId),
					include: q => q.Include(x => x.Method)
				)).ToList();

			var methodsByInspection = selections
				.GroupBy(s => s.InspectionId)
				.ToDictionary(
					g => g.Key,
					g => g.Select(s => s.Method)
						.Where(m => m != null)
						.OrderBy(m => m.SortOrder)
						.Select(m => new InspectionMethodResponse
						{
							Id = m.Id,
							Code = m.Code,
							Name = m.Name,
							SortOrder = m.SortOrder,
							IsActive = m.IsActive
						}).ToList()
				);

			var assets = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerId != null && inspectionIds.Contains(f.OwnerId.Value) && f.OwnerType == FileOwnerType.Inspection))
				.ToList();
			var mediaByInspection = assets
				.Where(a => a.OwnerId != null)
				.GroupBy(a => a.OwnerId!.Value)
				.ToDictionary(g => g.Key, g => g.ToList());

			return new InspectionFormResponse
			{
				Id = form.Id,
				TemplateId = form.TemplateId,
				TemplateName = form.Template?.Name ?? string.Empty,
				StaffId = form.CreatedByUserId,
				StaffName = form.Staff?.FullName,
				ItemType = form.ItemType,
				ItemId = form.ItemId,
				Type = form.Type,
				HandoverType = form.HandoverType,
				InspectionTypeId = form.InspectionTypeId,
				BranchId = form.BranchId,
				OverallPassed = form.OverallPassed,
				CreatedAt = form.CreatedAt,
				Rows = inspections
					.OrderBy(i => i.Section).ThenBy(i => i.Label)
					.Select(i => new InspectionFormRowResponse
					{
						InspectionId = i.Id,
						Section = i.Section,
						Label = i.Label,
						Passed = i.Passed,
						Notes = i.Notes,
						Methods = methodsByInspection.TryGetValue(i.Id, out var ms) ? ms : new List<InspectionMethodResponse>(),
						Media = mediaByInspection.TryGetValue(i.Id, out var md)
							? md.Select(a => new FileAssetDTO { Id = a.Id, Url = a.Url, ContentType = a.ContentType, SizeBytes = a.SizeBytes, Label = a.Label }).ToList()
							: new List<FileAssetDTO>()
					}).ToList()
			};
		}

		public async Task<List<InspectionFormSummaryResponse>> ListByBookingAsync(Guid bookingId)
		{
			var forms = (await _unitOfWork.Repository<InspectionForm>()
				.ListAsync(
					filter: f => f.Type == InspectionType.Booking && f.InspectionTypeId == bookingId,
					orderBy: q => q.OrderByDescending(f => f.CreatedAt),
					include: q => q.Include(f => f.Template).Include(f => f.Staff)
				)).ToList();

			return forms.Select(f => new InspectionFormSummaryResponse
			{
				Id = f.Id,
				TemplateId = f.TemplateId,
				TemplateName = f.Template?.Name ?? string.Empty,
				StaffId = f.CreatedByUserId,
				StaffName = f.Staff?.FullName,
				ItemType = f.ItemType,
				ItemId = f.ItemId,
				Type = f.Type,
				HandoverType = f.HandoverType,
				InspectionTypeId = f.InspectionTypeId,
				BranchId = f.BranchId,
				OverallPassed = f.OverallPassed,
				CreatedAt = f.CreatedAt
			}).ToList();
		}

		public async Task<List<InspectionFormSummaryResponse>> ListByVerificationAsync(Guid verificationId)
		{
			var forms = (await _unitOfWork.Repository<InspectionForm>()
				.ListAsync(
					filter: f => f.Type == InspectionType.Verification && f.InspectionTypeId == verificationId,
					orderBy: q => q.OrderByDescending(f => f.CreatedAt),
					include: q => q.Include(f => f.Template).Include(f => f.Staff)
				)).ToList();

			return forms.Select(f => new InspectionFormSummaryResponse
			{
				Id = f.Id,
				TemplateId = f.TemplateId,
				TemplateName = f.Template?.Name ?? string.Empty,
				StaffId = f.CreatedByUserId,
				StaffName = f.Staff?.FullName,
				ItemType = f.ItemType,
				ItemId = f.ItemId,
				Type = f.Type,
				HandoverType = f.HandoverType,
				InspectionTypeId = f.InspectionTypeId,
				BranchId = f.BranchId,
				OverallPassed = f.OverallPassed,
				CreatedAt = f.CreatedAt
			}).ToList();
		}

		public async Task<int> UpdateAsync(Guid formId, UpdateInspectionFormRequest request, Guid staffId)
		{
			var form = await _unitOfWork.Repository<InspectionForm>().GetByIdAsync(formId);
			if (form == null) return 0;

			var inspections = (await _unitOfWork.Repository<Inspection>()
				.ListAsync(i => i.FormId == formId)).ToList();
			var inspectionById = inspections.ToDictionary(i => i.Id, i => i);

			// validate all ids belong to this form
			foreach (var row in request.Rows)
			{
				if (!inspectionById.ContainsKey(row.InspectionId))
					throw new InvalidOperationException("One or more inspection rows do not belong to this form.");
			}

			// Validate methods are allowed by the template for each row
			var template = await _checklistService.GetTemplateByIdAsync(form.TemplateId);
			if (template == null) throw new InvalidOperationException("Checklist template not found.");

			static string Key(string section, string label)
				=> $"{(section ?? string.Empty).Trim().ToLowerInvariant()}|{(label ?? string.Empty).Trim().ToLowerInvariant()}";

			var allowedByRow = template.Sections
				.SelectMany(s => s.Items.Select(i => new
				{
					Key = Key(s.Name, i.Label),
					AllowedMethodIds = i.AllowedMethods.Select(m => m.Id).ToHashSet()
				}))
				.ToDictionary(x => x.Key, x => x.AllowedMethodIds);

			foreach (var row in request.Rows)
			{
				var inspection = inspectionById[row.InspectionId];
				var rowKey = Key(inspection.Section, inspection.Label);

				if (!allowedByRow.TryGetValue(rowKey, out var allowedMethodIds))
					throw new InvalidOperationException("One or more inspection rows are not part of the template.");

				foreach (var methodId in row.MethodIds.Distinct())
				{
					if (!allowedMethodIds.Contains(methodId))
						throw new InvalidOperationException($"Method is not allowed for checklist row: [{inspection.Section}] {inspection.Label}");
				}
			}

			await EnsureMethodsExistAsync(request.Rows.SelectMany(r => r.MethodIds));

			// update each row + replace method selections
			foreach (var row in request.Rows)
			{
				var inspection = inspectionById[row.InspectionId];
				inspection.Passed = row.Passed;
				inspection.Notes = row.Notes ?? string.Empty;
				inspection.UpdatedAt = DateTime.UtcNow;
				inspection.UpdatedByUserId = staffId;
				await _unitOfWork.Repository<Inspection>().UpdateAsync(inspection);

				var existingSelections = (await _unitOfWork.Repository<InspectionMethodSelection>()
					.ListAsync(s => s.InspectionId == inspection.Id)).ToList();
				foreach (var s in existingSelections)
				{
					await _unitOfWork.Repository<InspectionMethodSelection>().DeleteAsync(s.Id);
				}

				foreach (var methodId in row.MethodIds.Distinct())
				{
					await _unitOfWork.Repository<InspectionMethodSelection>().AddAsync(new InspectionMethodSelection
					{
						Id = Guid.NewGuid(),
						InspectionId = inspection.Id,
						MethodId = methodId,
						CreatedAt = DateTime.UtcNow,
						CreatedByUserId = staffId
					});
				}
			}

			// update overall + IsConfirmed
			var overallPassed = request.Passed ?? DeriveOverallPassed(inspections.Select(i => new SubmitChecklistRowRequest
			{
				Section = i.Section,
				Label = i.Label,
				MethodIds = new List<Guid>(),
				Passed = i.Passed,
				Notes = i.Notes
			}).ToList());

			form.OverallPassed = overallPassed;
			form.UpdatedAt = DateTime.UtcNow;
			form.UpdatedByUserId = staffId;
			await _unitOfWork.Repository<InspectionForm>().UpdateAsync(form);

			if (overallPassed != null)
			{
				await UpdateItemConfirmationAsync(form.ItemType, form.ItemId, overallPassed.Value);
			}

			return await _unitOfWork.Complete();
		}

		private static bool? DeriveOverallPassed(List<SubmitChecklistRowRequest> rows)
		{
			if (rows.Any(r => r.Passed == false)) return false;
			if (rows.Count > 0 && rows.All(r => r.Passed == true)) return true;
			return null;
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

		private async Task UpdateItemConfirmationAsync(ItemType itemType, Guid itemId, bool passed)
		{
			if (itemType == ItemType.Camera)
			{
				var camera = await _unitOfWork.Repository<Camera>().GetByIdAsync(itemId);
				if (camera != null)
				{
					camera.IsConfirmed = passed;
					await _unitOfWork.Repository<Camera>().UpdateAsync(camera);
				}
			}
			else if (itemType == ItemType.Accessory)
			{
				var accessory = await _unitOfWork.Repository<Accessory>().GetByIdAsync(itemId);
				if (accessory != null)
				{
					accessory.IsConfirmed = passed;
					await _unitOfWork.Repository<Accessory>().UpdateAsync(accessory);
				}
			}
		}
	}
}
