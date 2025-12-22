using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.MoneyPlatformSettingDTO;

namespace CamRent_Application.Services
{
	public sealed class MoneyPlatformSettingsService : IMoneyPlatformSettingsService
	{
		private readonly IUnitOfWork _uow;

		public MoneyPlatformSettingsService(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public async Task<IReadOnlyList<MoneyPlatformSettingResponse>> GetAllAsync(CancellationToken ct = default)
		{
			var list = await _uow.Repository<MoneyFlatformSetting>().ListAsync();
			return list
				.OrderByDescending(x => x.CreatedAt)
				.Select(Map)
				.ToList();
		}

		public async Task<MoneyPlatformSettingResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			var entity = await _uow.Repository<MoneyFlatformSetting>().GetByIdAsync(id);
			return entity == null ? null : Map(entity);
		}

		public async Task<MoneyPlatformSettingResponse?> GetActiveAsync(CancellationToken ct = default)
		{
			var active = (await _uow.Repository<MoneyFlatformSetting>().ListAsync(x => x.IsActive))
				.OrderByDescending(x => x.CreatedAt)
				.FirstOrDefault();
			return active == null ? null : Map(active);
		}

		public async Task<Guid> CreateAsync(MoneyPlatformSettingRequest req, Guid createdByUserId, CancellationToken ct = default)
		{
			Validate(req);

			// Nếu tạo record mới là active -> deactivate các record active khác
			if (req.IsActive)
			{
				var actives = await _uow.Repository<MoneyFlatformSetting>().ListAsync(x => x.IsActive);
				foreach (var a in actives)
				{
					a.IsActive = false;
					a.UpdatedByUserId = createdByUserId;
					a.UpdatedAt = DateTime.UtcNow;
					await _uow.Repository<MoneyFlatformSetting>().UpdateAsync(a);
				}
			}

			var entity = new MoneyFlatformSetting
			{
				Id = Guid.NewGuid(),
				UpfrontPercent = req.UpfrontPercent,
				PlatformFeePercent = req.PlatformFeePercent,
				OwnerSharePercent = req.OwnerSharePercent,
				LateFeeFirstNDays = req.LateFeeFirstNDays,
				LateFeeFactorFirstN = req.LateFeeFactorFirstN,
				LateFeeFactorAfter = req.LateFeeFactorAfter,
				DowntimeFactor = req.DowntimeFactor,
				CancelTime = TimeSpan.FromMinutes(req.CancelTimeMinutes),
				IsActive = req.IsActive,
				CreatedByUserId = createdByUserId
			};

			await _uow.Repository<MoneyFlatformSetting>().AddAsync(entity);
			await _uow.Complete();
			return entity.Id;
		}

		public async Task<bool> UpdateAsync(Guid id, MoneyPlatformSettingRequest req, Guid updatedByUserId, CancellationToken ct = default)
		{
			Validate(req);

			var repo = _uow.Repository<MoneyFlatformSetting>();
			var entity = await repo.GetByIdAsync(id);
			if (entity == null) return false;

			// Nếu chuyển sang active -> deactivate các record khác
			if (req.IsActive && !entity.IsActive)
			{
				var actives = await repo.ListAsync(x => x.IsActive && x.Id != id);
				foreach (var a in actives)
				{
					a.IsActive = false;
					a.UpdatedByUserId = updatedByUserId;
					a.UpdatedAt = DateTime.UtcNow;
					await repo.UpdateAsync(a);
				}
			}

			entity.UpfrontPercent = req.UpfrontPercent;
			entity.PlatformFeePercent = req.PlatformFeePercent;
			entity.OwnerSharePercent = req.OwnerSharePercent;
			entity.LateFeeFirstNDays = req.LateFeeFirstNDays;
			entity.LateFeeFactorFirstN = req.LateFeeFactorFirstN;
			entity.LateFeeFactorAfter = req.LateFeeFactorAfter;
			entity.DowntimeFactor = req.DowntimeFactor;
			entity.CancelTime = TimeSpan.FromMinutes(req.CancelTimeMinutes);
			entity.IsActive = req.IsActive;
			entity.UpdatedByUserId = updatedByUserId;
			entity.UpdatedAt = DateTime.UtcNow;

			await repo.UpdateAsync(entity);
			await _uow.Complete();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
		{
			var repo = _uow.Repository<MoneyFlatformSetting>();
			var entity = await repo.GetByIdAsync(id);
			if (entity == null) return false;

			await repo.DeleteAsync(id);
			await _uow.Complete();
			return true;
		}

		private static MoneyPlatformSettingResponse Map(MoneyFlatformSetting x) => new()
		{
			Id = x.Id,
			UpfrontPercent = x.UpfrontPercent,
			PlatformFeePercent = x.PlatformFeePercent,
			OwnerSharePercent = x.OwnerSharePercent,
			LateFeeFirstNDays = x.LateFeeFirstNDays,
			LateFeeFactorFirstN = x.LateFeeFactorFirstN,
			LateFeeFactorAfter = x.LateFeeFactorAfter,
			DowntimeFactor = x.DowntimeFactor,
			CancelTimeMinutes = (int)Math.Round(x.CancelTime.TotalMinutes),
			IsActive = x.IsActive,
			CreatedAt = x.CreatedAt,
			UpdatedAt = x.UpdatedAt
		};

		private static void Validate(MoneyPlatformSettingRequest req)
		{
			if (req.UpfrontPercent < 0 || req.UpfrontPercent > 1)
				throw new AppException("UpfrontPercent phải nằm trong [0..1]");
			if (req.PlatformFeePercent < 0 || req.PlatformFeePercent > 1)
				throw new AppException("PlatformFeePercent phải nằm trong [0..1]");
			if (req.OwnerSharePercent < 0 || req.OwnerSharePercent > 1)
				throw new AppException("OwnerSharePercent phải nằm trong [0..1]");
			if (req.LateFeeFirstNDays < 0)
				throw new AppException("LateFeeFirstNDays phải >= 0");
			if (req.LateFeeFactorFirstN < 0)
				throw new AppException("LateFeeFactorFirstN phải >= 0");
			if (req.LateFeeFactorAfter < 0)
				throw new AppException("LateFeeFactorAfter phải >= 0");
			if (req.DowntimeFactor < 0)
				throw new AppException("DowntimeFactor phải >= 0");
			if (req.CancelTimeMinutes < 0)
				throw new AppException("CancelTimeMinutes phải >= 0");
		}
	}
}

