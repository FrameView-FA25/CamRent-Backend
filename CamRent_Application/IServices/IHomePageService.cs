using CamRent_Application.DTOs;
using Microsoft.AspNetCore.Http;
using static CamRent_Application.DTOs.HomePageDTO;

namespace CamRent_Application.IServices
{
	public interface IHomePageService
	{
		Task<IReadOnlyList<CarouselItemResponse>> GetCarouselAsync(bool includeInactive, CancellationToken ct = default);
		Task<Guid> CreateCarouselItemAsync(string title, string content, string? linkUrl, int sortOrder, bool isActive, IFormFile image, Guid createdByUserId, CancellationToken ct = default);
		Task<bool> UpdateCarouselItemAsync(Guid id, string? title, string? content, string? linkUrl, int? sortOrder, bool? isActive, IFormFile? image, Guid updatedByUserId, CancellationToken ct = default);
		Task<bool> ReorderCarouselAsync(IReadOnlyList<ReorderCarouselItemRequest> items, Guid updatedByUserId, CancellationToken ct = default);
		Task<bool> DeleteCarouselItemAsync(Guid id, CancellationToken ct = default);
	}
}

