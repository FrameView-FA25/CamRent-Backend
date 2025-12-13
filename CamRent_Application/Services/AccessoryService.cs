using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CamRent_Application.Common;
using static CamRent_Application.DTOs.AccessoryDTO;

namespace CamRent_Application.Services
{
	public class AccessoryService : IAccessoryService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IIndexingService _indexing;
		public AccessoryService(IUnitOfWork unitOfWork, IMapper mapper, IIndexingService indexing)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_indexing = indexing;
		}
		public async Task<int> CreateAccessoryAsync(Accessory accessory)
		{
			await _unitOfWork.Repository<Accessory>().AddAsync(accessory);
			var result = await _unitOfWork.Complete();
			if (result > 0) _indexing.EnqueueUpsert("Accessory", accessory.Id);
			return result;
		}

		public async Task<int> DeleteAccessoryAsync(Guid accessoryId)
		{
			await _unitOfWork.Repository<Accessory>().DeleteAsync(accessoryId);
			var result = await _unitOfWork.Complete();
			if (result > 0) _indexing.EnqueueDelete("Accessory", accessoryId);
			return result;
		}

		public async Task<List<AccessoryResponseDTO>> GetAccessoriesByOwnerIdAsync(Guid userId)
		{
			var accessories = await _unitOfWork.Repository<Accessory>().ListAsync(filter: a => a.OwnerUserId == userId, include: a => a.Include(b => b.Branch));
			foreach (var accessory in accessories)
			{
				var media = (await _unitOfWork.Repository<FileAsset>().ListAsync(filter: f => f.OwnerId == accessory.Id && f.OwnerType == FileOwnerType.Accessory)).ToList();
				accessory.Media = media;
			}
			var accessoryResponseDTOs = _mapper.Map<List<AccessoryResponseDTO>>(accessories);
			return accessoryResponseDTOs;
		}

		public async Task<AccessoryResponseDTO?> GetAccessoryByIdAsync(Guid accessoryId)
		{
			var accessory = (await  _unitOfWork.Repository<Accessory>().ListAsync(filter: a => a.Id == accessoryId, include: a => a.Include(b => b.Branch))).FirstOrDefault();
			accessory.Media = (await _unitOfWork.Repository<FileAsset>().ListAsync(filter: f => f.OwnerId == accessory.Id && f.OwnerType == FileOwnerType.Accessory)).ToList();
			var accessoryResponseDTO = _mapper.Map<AccessoryResponseDTO>(accessory);
			return accessoryResponseDTO;
		}

		public async Task<List<AccessoryResponseDTO>> GetAllAccessoriesAsync()
		{
			var accessories = await _unitOfWork.Repository<Accessory>().ListAsync(include: a => a.Include(b => b.Branch));
			foreach (var accessory in accessories)
			{
				var media = (await _unitOfWork.Repository<FileAsset>().ListAsync(filter: f => f.OwnerId == accessory.Id && f.OwnerType == FileOwnerType.Accessory)).ToList();
				accessory.Media = media;
			}
			var accessoryResponseDTOs = _mapper.Map<List<AccessoryResponseDTO>>(accessories);
			return accessoryResponseDTOs;
		}

		public async Task<int> UpdateAccessoryAsync(UpdateAccessoryRequest request, Guid userId)
		{
			var accessory = await _unitOfWork.Repository<Accessory>().GetByIdAsync(request.Id);
			if (accessory == null) return 0;
			_mapper.Map(request, accessory);
			accessory.UpdatedAt = DateTime.UtcNow;
			accessory.UpdatedByUserId = userId;
			await _unitOfWork.Repository<Accessory>().UpdateAsync(accessory);
			var result = await _unitOfWork.Complete();
			if (result > 0) _indexing.EnqueueUpsert("Accessory", accessory.Id);
			return result;
		}

		public async Task<AccessoryHistoryDTO> GetHistoryForQrAsync(Guid accessoryId)
		{
			// Phụ kiện
			var accessory = (await _unitOfWork.Repository<Accessory>()
				.ListAsync(filter: a => a.Id == accessoryId,
					include: a => a.Include(a => a.Branch)))
				.FirstOrDefault() ?? throw new InvalidOperationException("Accessory not found");

			accessory.Media = (await _unitOfWork.Repository<FileAsset>()
				.ListAsync(f => f.OwnerType == FileOwnerType.Accessory && f.OwnerId == accessory.Id)).ToList();

			var accessoryDto = _mapper.Map<AccessoryResponseDTO>(accessory);

			// Booking history đơn giản
			var bookingItems = await _unitOfWork.Repository<BookingItem>()
				.ListAsync(bi => bi.AccessoryId == accessoryId,
					include: q => q
						.Include(bi => bi.Booking)!.ThenInclude(b => b.Renter));

			var bookingHistory = bookingItems
				.Where(bi => bi.Booking != null)
				.Select(bi => bi.Booking!)
				.Distinct()
				.OrderByDescending(b => b.PickupAt)
				.Take(10)
				.Select(b => new AccessoryBookingHistoryItem
				{
					BookingId = b.Id,
					PickupAt = b.PickupAt,
					ReturnAt = b.ReturnAt,
					Status = b.Status,
					StatusText = b.Status.GetDisplayName(),
					RenterName = b.Renter?.FullName
				})
				.ToList();

			// Inspections liên quan tới phụ kiện
			var inspections = await _unitOfWork.Repository<Inspection>()
				.ListAsync(i => i.AccessoryId == accessoryId);
			var inspectionDtos = _mapper.Map<List<InspectionDTO.InspectionResponseDTO>>(inspections);

			return new AccessoryHistoryDTO
			{
				Accessory = accessoryDto,
				Bookings = bookingHistory,
				Inspections = inspectionDtos
			};
		}
	}
}
