using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
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
		public AccessoryService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}
		public async Task<int> CreateAccessoryAsync(Accessory accessory)
		{
			await _unitOfWork.Repository<Accessory>().AddAsync(accessory);
			return await _unitOfWork.Complete();
		}

		public async Task<int> DeleteAccessoryAsync(Guid accessoryId)
		{
			await _unitOfWork.Repository<Accessory>().DeleteAsync(accessoryId);
			return await _unitOfWork.Complete();
		}

		public async Task<AccessoryResponseDTO?> GetAccessoryByIdAsync(Guid accessoryId)
		{
			var accessory = (await  _unitOfWork.Repository<Accessory>().ListAsync(filter: a => a.Id == accessoryId, include: a => a.Include(b => b.Branch).Include(c => c.Media))).FirstOrDefault();
			var accessoryResponseDTO = _mapper.Map<AccessoryResponseDTO>(accessory);
			return accessoryResponseDTO;
		}

		public async Task<List<AccessoryResponseDTO>> GetAllAccessoriesAsync()
		{
			var accessories = await _unitOfWork.Repository<Accessory>().ListAsync(include: a => a.Include(b => b.Branch).Include(c => c.Media));
			var accessoryResponseDTOs = _mapper.Map<List<AccessoryResponseDTO>>(accessories);
			return accessoryResponseDTOs;
		}

		public async Task<int> UpdateAccessoryAsync(Accessory accessory)
		{
			await _unitOfWork.Repository<Accessory>().UpdateAsync(accessory);
			return await _unitOfWork.Complete();
		}
	}
}
