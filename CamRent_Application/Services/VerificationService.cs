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
			verification.CreatedByUserId = ownerId;
			verification.CreatedAt = DateTime.UtcNow;
			await _unitOfWork.Repository<VerificationRequest>().AddAsync(verification);
			var result = await _unitOfWork.Complete();
			return result;
		}

		public async Task<List<VerificationResponseDTO>> GetVerificationByManagerId(Guid id)
		{
			var verifications = await _unitOfWork.Repository<VerificationRequest>().ListAsync(
				include: v => v.Include(v => v.Branch).Include(v => v.Staff),
				filter: v => v.Branch.ManagerId == id);
			var result =  _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return result;
		}

		public async Task<List<VerificationResponseDTO>> GetVerificationByOwnerId(Guid id)
		{
			var verifications = await _unitOfWork.Repository<VerificationRequest>().ListAsync(
				filter: v => v.CreatedByUserId == id,
				include: v => v.Include(v => v.Branch).Include(v => v.Staff));
			var result =  _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return result;
		}

		public async Task<List<VerificationResponseDTO>> GetVerificationByStaffId(Guid id)
		{
			var verifications = await _unitOfWork.Repository<VerificationRequest>().ListAsync(
				filter: v => v.StaffId == id,
				include: v => v.Include(v => v.Branch).Include(v => v.Staff));
			var result =  _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return result;
		}

		public async Task<List<VerificationResponseDTO>> GetVerifications()
		{
			var verifications = _unitOfWork.Repository<VerificationRequest>().ListAsync(include: v => v.Include(v => v.Branch).Include(v => v.Staff));
			var result =  _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return result;
		}
	}
}
