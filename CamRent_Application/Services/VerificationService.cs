using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
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
				item.VerificationRequest = verification;
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

	}
}
