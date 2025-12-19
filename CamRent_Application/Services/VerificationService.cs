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
	/// <summary>
	/// Xử lý toàn bộ nghiệp vụ liên quan đến Verification:
	/// - Tạo/cập nhật/xóa yêu cầu verification cho thiết bị của owner
	/// - Gán staff, load chi tiết verification cho Owner/Manager/Staff
	/// - Cập nhật trạng thái verification và ký hợp đồng liên quan.
	/// </summary>
	public class VerificationService : IVerificationService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		public VerificationService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		/// <summary>
		/// Gán một staff cụ thể vào verification request.
		/// Dùng khi Manager phân công người đi kiểm tra thiết bị.
		/// </summary>
		public async Task<int> AssignStaffToVerification(Guid staffId, Guid verificationRequest)
		{
			var verification = await  _unitOfWork.Repository<VerificationRequest>().GetByIdAsync(verificationRequest);
			verification.StaffId = staffId;
			await _unitOfWork.Repository<VerificationRequest>().UpdateAsync(verification);
			var result = await _unitOfWork.Complete();
			return result;
		}

		/// <summary>
		/// Tạo mới một verification request cho owner:
		/// - Map DTO sang entity
		/// - Gán owner tạo yêu cầu
		/// - Lưu các item (thiết bị cần verification) kèm theo.
		/// </summary>
		public async Task<Guid> CreateVerificationAsync(CreateVerificationRequestDTO verificationRequestDTO, Guid ownerId)
		{
			var verification = _mapper.Map<VerificationRequest>(verificationRequestDTO);
			if(verification.Items == null)
			{
				return Guid.Empty;
			}	
			verification.CreatedByUserId = ownerId;
			verification.CreatedAt = DateTime.UtcNow;
			verification.Status = VerificationStatus.Pending;

			// đảm bảo EF hiểu quan hệ cha–con (nếu bạn dùng navigation)
			foreach (var item in verification.Items)
			{
				await _unitOfWork.Repository<VerificationRequestItem>().AddAsync(item);
				// KHÔNG cần gán VerificationId, EF sẽ tự set sau khi insert
			}

			await _unitOfWork.Repository<VerificationRequest>().AddAsync(verification);
			await _unitOfWork.Complete();
			return verification.Id;
		}

		/// <summary>
		/// Thiết bị (camera/phụ kiện) của owner chưa được xác minh (IsConfirmed = false).
		/// Dùng cho màn tạo verification để lọc đúng thiết bị thuộc sở hữu owner.
		/// </summary>
		public async Task<List<VerificationItemDTO>> GetUnverifiedDevicesForOwnerAsync(Guid ownerId)
		{
			var cameras = (await _unitOfWork.Repository<Camera>()
				.ListAsync(c => c.OwnerUserId == ownerId && !c.IsConfirmed)).ToList();
			var accessories = (await _unitOfWork.Repository<Accessory>()
				.ListAsync(a => a.OwnerUserId == ownerId && !a.IsConfirmed)).ToList();
			var verifications = await _unitOfWork.Repository<VerificationRequest>()
				.ListAsync(v => v.CreatedByUserId == ownerId && v.Status == VerificationStatus.Pending,
					include: q => q.Include(v => v.Items));
			foreach (var ver in verifications)
			{
				foreach (var item in ver.Items)
				{
					if (item.CameraId != null)
					{
						cameras.RemoveAll(c => c.Id == item.CameraId.Value);
					}
					else if (item.AccessoryId != null)
					{
						accessories.RemoveAll(a => a.Id == item.AccessoryId.Value);
					}
				}
			}

			var result = new List<VerificationItemDTO>();

			result.AddRange(cameras.Select(c => new VerificationItemDTO
			{
				ItemId = c.Id,
				ItemName = $"{c.Brand} {c.Model}",
				ItemType = ItemType.Camera
			}));

			result.AddRange(accessories.Select(a => new VerificationItemDTO
			{
				ItemId = a.Id,
				ItemName = $"{a.Brand} {a.Model}",
				ItemType = ItemType.Accessory
			}));

			return result;
		}

		/// <summary>
		/// Lấy danh sách verification thuộc các branch mà Manager đang quản lý,
		/// bao gồm đầy đủ navigation (branch, staff, inspections, items, contracts).
		/// </summary>
		public async Task<List<VerificationResponseDTO>> GetVerificationByManagerId(Guid managerId)
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.Branch != null && v.Branch.ManagerId == managerId,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)

						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
						.Include(b => b.Contracts).ThenInclude(c => c.Signatures)
				);

			var response = _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return response;
		}

		/// <summary>
		/// Lấy danh sách verification do một owner tạo.
		/// </summary>
		public async Task<List<VerificationResponseDTO>> GetVerificationByOwnerId(Guid ownerId)
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.CreatedByUserId == ownerId,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)

						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
						.Include(b => b.Contracts).ThenInclude(c => c.Signatures)
				);

			var response = _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return response;
		}


		/// <summary>
		/// Lấy danh sách verification được gán cho một staff cụ thể để xử lý.
		/// </summary>
		public async Task<List<VerificationResponseDTO>> GetVerificationByStaffId(Guid staffId)
		{
			var verifications = await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.StaffId == staffId,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)

						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)
						.Include(b => b.Contracts).ThenInclude(c => c.Signatures)
				);
			var response = _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return response;
		}


		/// <summary>
		/// Lấy toàn bộ verification trong hệ thống (dùng cho màn quản trị tổng quan).
		/// </summary>
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

						.Include(b => b.Contracts).ThenInclude(c => c.Signatures)
				);

			var response = _mapper.Map<List<VerificationResponseDTO>>(verifications);
			return response;
		}

		/// <summary>
		/// Lấy chi tiết một verification theo Id,
		/// bao gồm inspections, items, contracts/signatures và media liên quan.
		/// </summary>
		public async Task<VerificationResponseDTO?> GetVerificationById(Guid id)
		{
			var verification = (await _unitOfWork
				.Repository<VerificationRequest>()
				.ListAsync(
					filter: v => v.Id == id,
					include: q => q
						.Include(v => v.Branch)
						.Include(v => v.Staff)
						.Include(v => v.Items).ThenInclude(i => i.Camera)
						.Include(v => v.Items).ThenInclude(i => i.Accessory)

						.Include(b => b.Contracts).ThenInclude(c => c.Signatures)
				)).FirstOrDefault();
			if (verification == null) return null;
			var response = _mapper.Map<VerificationResponseDTO>(verification);


			return response;
		}

		/// <summary>
		/// Cập nhật một verification:
		/// - Map các field được phép sửa từ DTO sang entity
		/// - Thay thế toàn bộ danh sách items nếu client gửi kèm Items mới.
		/// </summary>
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

		/// <summary>
		/// Cập nhật trạng thái của verification (Pending/Approved/Rejected/...).
		/// Nếu status = Approved, đồng thời tự động ký hợp đồng bởi Platform Manager
		/// bằng cách gán chữ ký của manager vào ContractSignature tương ứng.
		/// </summary>
		public async Task<int> UpdateVerificationStatusAsync(Guid id, Guid managerId, string note, VerificationStatus status)
		{
			var verification = await _unitOfWork.Repository<VerificationRequest>().GetByIdAsync(id);
			if (verification == null) return 0;
			verification.Status = status;
			verification.Notes = note;
			if (status == VerificationStatus.Approved)
			{
				var manager = await _unitOfWork.Repository<User>().GetByIdAsync(managerId);
				var contract = await _unitOfWork.Repository<Contract>().FirstOrDefaultAsync(c => c.VerificationId == id && c.Status == ContractStatus.PendingSignatures);
				var managerSigner = await _unitOfWork.Repository<ContractSignature>().FirstOrDefaultAsync(s => s.UserId == managerId && s.Role == ContractSignerRole.Platform && s.ContractId == contract.Id);

				managerSigner.SignatureAssetId = manager.SignatureAssetId;
				managerSigner.SignedAt = DateTime.UtcNow;
				managerSigner.IsSigned = true;
			}
			await _unitOfWork.Repository<VerificationRequest>().UpdateAsync(verification);
			return await _unitOfWork.Complete();
		}
		/// <summary>
		/// Xóa một verification cùng toàn bộ items con của nó.
		/// </summary>
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
