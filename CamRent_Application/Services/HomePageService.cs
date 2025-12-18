using CamRent_Application.DTOs;
using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static CamRent_Application.DTOs.HomePageDTO;

namespace CamRent_Application.Services
{
	public sealed class HomePageService : IHomePageService
	{
		private readonly IUnitOfWork _uow;
		private readonly IFileStorageService _files;

		public HomePageService(IUnitOfWork uow, IFileStorageService files)
		{
			_uow = uow;
			_files = files;
		}

		public async Task<IReadOnlyList<CarouselItemResponse>> GetCarouselAsync(bool includeInactive, CancellationToken ct = default)
		{
			var items = await _uow.Repository<HomePageCarouselItem>().ListAsync(
				filter: includeInactive ? null : (x => x.IsActive),
				include: q => q.Include(x => x.ImageAsset));

			return items
				.OrderBy(x => x.SortOrder)
				.ThenByDescending(x => x.CreatedAt)
				.Select(x => new CarouselItemResponse
				{
					Id = x.Id,
					Title = x.Title,
					Content = x.Content,
					LinkUrl = x.LinkUrl,
					SortOrder = x.SortOrder,
					IsActive = x.IsActive,
					ImageUrl = x.ImageAsset?.Url
				})
				.ToList();
		}

		public async Task<Guid> CreateCarouselItemAsync(
			string title,
			string content,
			string? linkUrl,
			int sortOrder,
			bool isActive,
			Microsoft.AspNetCore.Http.IFormFile image,
			Guid createdByUserId,
			CancellationToken ct = default)
		{
			var item = new HomePageCarouselItem
			{
				Id = Guid.NewGuid(),
				Title = title.Trim(),
				Content = content.Trim(),
				LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim(),
				SortOrder = sortOrder,
				IsActive = isActive,
				CreatedByUserId = createdByUserId
			};

			await _uow.Repository<HomePageCarouselItem>().AddAsync(item);
			await _uow.Complete();

			var asset = await _files.UploadAsync(
				image,
				ownerId: item.Id,
				ownerType: FileOwnerType.HomePageCarousel,
				folder: $"camrent/homepage/carousel/{item.Id}",
				label: item.Title);

			item.ImageAssetId = asset.Id;
			await _uow.Repository<HomePageCarouselItem>().UpdateAsync(item);
			await _uow.Complete();

			return item.Id;
		}

		public async Task<bool> UpdateCarouselItemAsync(
			Guid id,
			string? title,
			string? content,
			string? linkUrl,
			int? sortOrder,
			bool? isActive,
			Microsoft.AspNetCore.Http.IFormFile? image,
			Guid updatedByUserId,
			CancellationToken ct = default)
		{
			var repo = _uow.Repository<HomePageCarouselItem>();
			var item = await repo.GetByIdAsync(id);
			if (item == null) return false;

			if (!string.IsNullOrWhiteSpace(title)) item.Title = title.Trim();
			if (content != null) item.Content = content.Trim();
			if (linkUrl != null) item.LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
			if (sortOrder.HasValue) item.SortOrder = sortOrder.Value;
			if (isActive.HasValue) item.IsActive = isActive.Value;

			item.UpdatedByUserId = updatedByUserId;
			item.UpdatedAt = DateTime.UtcNow;

			if (image != null)
			{
				if (item.ImageAssetId.HasValue)
				{
					await _files.DeleteByAssetIdAsync(item.ImageAssetId.Value);
				}

				var asset = await _files.UploadAsync(
					image,
					ownerId: item.Id,
					ownerType: FileOwnerType.HomePageCarousel,
					folder: $"camrent/homepage/carousel/{item.Id}",
					label: item.Title);

				item.ImageAssetId = asset.Id;
			}

			await repo.UpdateAsync(item);
			await _uow.Complete();
			return true;
		}

		public async Task<bool> ReorderCarouselAsync(IReadOnlyList<ReorderCarouselItemRequest> items, Guid updatedByUserId, CancellationToken ct = default)
		{
			if (items == null || items.Count == 0) return true;

			var repo = _uow.Repository<HomePageCarouselItem>();
			var ids = items.Select(x => x.Id).Distinct().ToList();
			var dbItems = await repo.ListAsync(x => ids.Contains(x.Id));

			var lookup = items.ToDictionary(x => x.Id, x => x.SortOrder);
			foreach (var it in dbItems)
			{
				if (!lookup.TryGetValue(it.Id, out var order)) continue;
				it.SortOrder = order;
				it.UpdatedByUserId = updatedByUserId;
				it.UpdatedAt = DateTime.UtcNow;
				await repo.UpdateAsync(it);
			}

			await _uow.Complete();
			return true;
		}

		public async Task<bool> DeleteCarouselItemAsync(Guid id, CancellationToken ct = default)
		{
			var repo = _uow.Repository<HomePageCarouselItem>();
			var item = await repo.GetByIdAsync(id);
			if (item == null) return false;

			if (item.ImageAssetId.HasValue)
			{
				await _files.DeleteByAssetIdAsync(item.ImageAssetId.Value);
			}

			await repo.DeleteAsync(id);
			await _uow.Complete();
			return true;
		}

		public async Task<IReadOnlyList<BlockResponse>> GetBlocksAsync(bool includeInactive, CancellationToken ct = default)
		{
			var items = await _uow.Repository<HomePageBlock>().ListAsync(
				filter: includeInactive ? null : (x => x.IsActive),
				include: q => q.Include(x => x.ImageAsset));

			return items
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Key)
				.Select(MapBlock)
				.ToList();
		}

		public async Task<BlockResponse?> GetBlockByKeyAsync(string key, bool includeInactive, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(key)) return null;
			var normalizedKey = key.Trim().ToLowerInvariant();

			var items = await _uow.Repository<HomePageBlock>().ListAsync(
				filter: includeInactive
					? (x => x.Key.ToLower() == normalizedKey)
					: (x => x.IsActive && x.Key.ToLower() == normalizedKey),
				include: q => q.Include(x => x.ImageAsset));

			var block = items.FirstOrDefault();
			return block == null ? null : MapBlock(block);
		}

		public async Task<Guid> UpsertBlockAsync(
			string key,
			string title,
			string content,
			int sortOrder,
			bool isActive,
			Microsoft.AspNetCore.Http.IFormFile? image,
			Guid updatedByUserId,
			CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new AppException("Key is required");

			var normalizedKey = key.Trim().ToLowerInvariant();
			var repo = _uow.Repository<HomePageBlock>();

			// tìm theo key (không phân biệt hoa thường)
			var existing = (await repo.ListAsync(x => x.Key.ToLower() == normalizedKey)).FirstOrDefault();

			if (existing == null)
			{
				existing = new HomePageBlock
				{
					Id = Guid.NewGuid(),
					Key = normalizedKey,
					Title = title?.Trim() ?? string.Empty,
					Content = content?.Trim() ?? string.Empty,
					SortOrder = sortOrder,
					IsActive = isActive,
					CreatedByUserId = updatedByUserId
				};
				await repo.AddAsync(existing);
				await _uow.Complete();
			}
			else
			{
				existing.Title = title?.Trim() ?? existing.Title;
				existing.Content = content?.Trim() ?? existing.Content;
				existing.SortOrder = sortOrder;
				existing.IsActive = isActive;
				existing.UpdatedByUserId = updatedByUserId;
				existing.UpdatedAt = DateTime.UtcNow;
				await repo.UpdateAsync(existing);
				await _uow.Complete();
			}

			if (image != null)
			{
				if (existing.ImageAssetId.HasValue)
				{
					await _files.DeleteByAssetIdAsync(existing.ImageAssetId.Value);
				}

				var asset = await _files.UploadAsync(
					image,
					ownerId: existing.Id,
					ownerType: FileOwnerType.HomePageBlock,
					folder: $"camrent/homepage/blocks/{existing.Key}",
					label: existing.Title);

				existing.ImageAssetId = asset.Id;
				existing.UpdatedByUserId = updatedByUserId;
				existing.UpdatedAt = DateTime.UtcNow;
				await repo.UpdateAsync(existing);
				await _uow.Complete();
			}

			return existing.Id;
		}

		public async Task<bool> DeleteBlockAsync(string key, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(key)) return false;
			var normalizedKey = key.Trim().ToLowerInvariant();

			var repo = _uow.Repository<HomePageBlock>();
			var block = (await repo.ListAsync(x => x.Key.ToLower() == normalizedKey)).FirstOrDefault();
			if (block == null) return false;

			if (block.ImageAssetId.HasValue)
			{
				await _files.DeleteByAssetIdAsync(block.ImageAssetId.Value);
			}

			await repo.DeleteAsync(block.Id);
			await _uow.Complete();
			return true;
		}

		public async Task<IReadOnlyList<HomePageFeedbackResponse>> GetFeedbackAsync(int limit, CancellationToken ct = default)
		{
			var take = limit <= 0 ? 3 : Math.Min(limit, 50);

			var reviews = await _uow.Repository<Review>().ListAsync(
				filter: r => r.Status == ReviewStatus.Approved,
				include: q => q.Include(r => r.AuthorUser).ThenInclude(u => u.Avatar));

			return reviews
				.OrderByDescending(r => r.CreatedAt)
				.Take(take)
				.Select(r => new HomePageFeedbackResponse
				{
					Id = r.Id,
					Rating = r.Rating,
					Content = r.Content,
					CreatedAt = r.CreatedAt,
					AuthorUserId = r.AuthorUserId,
					AuthorName = r.AuthorUser?.FullName ?? string.Empty,
					AuthorAvatarUrl = r.AuthorUser?.Avatar?.Url
				})
				.ToList();
		}

		private static BlockResponse MapBlock(HomePageBlock x) => new()
		{
			Id = x.Id,
			Key = x.Key,
			Title = x.Title,
			Content = x.Content,
			SortOrder = x.SortOrder,
			IsActive = x.IsActive,
			ImageUrl = x.ImageAsset?.Url
		};
	}
}

