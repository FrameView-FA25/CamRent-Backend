using static CamRent_Application.DTOs.MoneyPlatformSettingDTO;

namespace CamRent_Application.IServices
{
	public interface IMoneyPlatformSettingsService
	{
		Task<IReadOnlyList<MoneyPlatformSettingResponse>> GetAllAsync(CancellationToken ct = default);
		Task<MoneyPlatformSettingResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
		Task<MoneyPlatformSettingResponse?> GetActiveAsync(CancellationToken ct = default);
		Task<Guid> CreateAsync(MoneyPlatformSettingRequest req, Guid createdByUserId, CancellationToken ct = default);
		Task<bool> UpdateAsync(Guid id, MoneyPlatformSettingRequest req, Guid updatedByUserId, CancellationToken ct = default);
		Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
	}
}

