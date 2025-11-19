using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static CamRent_Application.DTOs.VerificationRequestDTO;

namespace CamRent_Application.Services
{
	public class VerificationService : IVerificationService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		public VerificationService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<int> AssignStaffToVerification(Guid staffId, Guid verificationRequest)
		{
			var verification = await  _unitOfWork.Repository<VerificationRequest>().GetByIdAsync(verificationRequest);
			verification.StaffId = staffId;
			await _unitOfWork.Repository<VerificationRequest>().UpdateAsync(verification);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<int> CreateVerificationAsync(CreateVerificationRequestDTO verificationRequestDTO, Guid ownerId)
		{
			var verification = _mapper.Map<VerificationRequest>(verificationRequestDTO);
			if(verification.Items == null)
			{
				return 0;
			}	
			verification.CreatedByUserId = ownerId;
			verification.CreatedAt = DateTime.UtcNow;

			// đảm bảo EF hiểu quan hệ cha–con (nếu bạn dùng navigation)
			foreach (var item in verification.Items)
			{
				await _unitOfWork.Repository<VerificationRequestItem>().AddAsync(item);
				// KHÔNG cần gán VerificationId, EF sẽ tự set sau khi insert
			}

			await _unitOfWork.Repository<VerificationRequest>().AddAsync(verification);
			var result = await _unitOfWork.Complete();
			return result;
		}
		

		public async Task<List<VerificationResponseDTO>> GetVerificationByManagerId(Guid managerId)
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.Branch != null && v.Branch.ManagerId == managerId,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)
						.Include(v => v.Inspections)
						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
				);

			return _mapper.Map<List<VerificationResponseDTO>>(verifications);
		}

		public async Task<List<VerificationResponseDTO>> GetVerificationByOwnerId(Guid ownerId)
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.CreatedByUserId == ownerId,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)
						.Include(v => v.Inspections)
						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
				);

			return _mapper.Map<List<VerificationResponseDTO>>(verifications);
		}


		public async Task<List<VerificationResponseDTO>> GetVerificationByStaffId(Guid staffId)
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.StaffId == staffId,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)
						.Include(v => v.Inspections)
						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
				);

			return _mapper.Map<List<VerificationResponseDTO>>(verifications);
		}


		public async Task<List<VerificationResponseDTO>> GetVerifications()
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)
						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
						.Include(v => v.Inspections)
				);

			return _mapper.Map<List<VerificationResponseDTO>>(verifications);
		}

		// New: get detail by id
		public async Task<VerificationResponseDTO?> GetVerificationById(Guid id)
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.Id == id,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)
						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
						.Include(v => v.Inspections)
				);

			var verification = verifications.FirstOrDefault();
			if (verification == null) return null;
			return _mapper.Map<VerificationResponseDTO>(verification);
		}

		// New: update verification
		public async Task<int> UpdateVerificationAsync(Guid id, UpdateVerificationRequestDTO request)
		{
			// load with items to be able to remove children if needed
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.Id == id,
					include: q => q.Include(v => v.Items)
				);

			var verification = verifications.FirstOrDefault();
			if (verification == null) return 0;

			_mapper.Map(request, verification); // maps only non-null fields thanks to Condition in profile

			// handle items replacement (simple strategy: delete existing and add new)
			if (request.Items != null)
			{
				if (verification.Items != null)
				{
					foreach (var existing in verification.Items.ToList())
					{
						await _unitOfWork.Repository<VerificationRequestItem>().DeleteAsync(existing.Id);
					}
				}

				verification.Items = new List<VerificationRequestItem>();
				foreach (var itemDto in request.Items)
				{
					var itemEntity = new VerificationRequestItem
					{
						VerificationId = verification.Id,
					};

					if (itemDto.ItemType == ItemType.Camera)
					{
						itemEntity.CameraId = itemDto.ItemId;
					}
					else if (itemDto.ItemType == ItemType.Accessory)
					{
						itemEntity.AccessoryId = itemDto.ItemId;
					}
					// if Combo or others, set as appropriate (left null-safe)

					await _unitOfWork.Repository<VerificationRequestItem>().AddAsync(itemEntity);
					verification.Items.Add(itemEntity);
				}
			}

			await _unitOfWork.Repository<VerificationRequest>().UpdateAsync(verification);
			return await _unitOfWork.Complete();
		}

		public async Task<int> UpdateVerificationStatusAsync(Guid id, string note, VerificationStatus status)
		{
			var verification = await _unitOfWork.Repository<VerificationRequest>().GetByIdAsync(id);
			if (verification == null) return 0;
			verification.Status = status;
			verification.Notes = note;
			await _unitOfWork.Repository<VerificationRequest>().UpdateAsync(verification);
			return await _unitOfWork.Complete();
		}
		// New: delete verification
		public async Task<int> DeleteVerificationAsync(Guid id)
		{
			// load with items to delete children explicitly if needed
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.Id == id,
					include: q => q.Include(v => v.Items)
				);

			var verification = verifications.FirstOrDefault();
			if (verification == null) return 0;

			if (verification.Items != null)
			{
				foreach (var item in verification.Items.ToList())
				{
					await _unitOfWork.Repository<VerificationRequestItem>().DeleteAsync(item.Id);
				}
			}

			await _unitOfWork.Repository<VerificationRequest>().DeleteAsync(id);
			return await _unitOfWork.Complete();
		}
	}
}
