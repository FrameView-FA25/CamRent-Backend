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
using System.Text;
using System.Threading.Tasks;
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

		public async Task<int> UpdateAccessoryAsync(Accessory accessory)
		{
			await _unitOfWork.Repository<Accessory>().UpdateAsync(accessory);
			var result = await _unitOfWork.Complete();
			if (result > 0) _indexing.EnqueueUpsert("Accessory", accessory.Id);
			return result;
		}
	}
}
