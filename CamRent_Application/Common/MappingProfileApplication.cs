using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.AccessoryDTO;
using static CamRent_Application.DTOs.BookingDTO;
using static CamRent_Application.DTOs.BranchDTO;
using static CamRent_Application.DTOs.CameraDTO;
using static CamRent_Application.DTOs.ContractDTO;
using static CamRent_Application.DTOs.InspectionDTO;
using static CamRent_Application.DTOs.VerificationRequestDTO;
using static CamRent_Application.DTOs.WalletDTO;

namespace CamRent_Application.Common
{
	public class MappingProfileApplication : Profile
	{
		public MappingProfileApplication()
		{
			CreateMap<Camera, CameraResponseDTO>()
				.ForMember(c => c.BranchName,
					opt => opt.MapFrom(s => s.Branch.Name))
				.ForMember(c => c.BranchAddress,
					opt => opt.MapFrom(s => s.Branch.Address.District + "," + s.Branch.Address.Province))
				.ForMember(c => c.OwnerName,
					opt => opt.MapFrom(s => s.OwnerUser.FullName));
			CreateMap<UpdateCameraRequest, Camera>()
				.ForMember(x => x.Id, opt => opt.Ignore())
				.ForMember(x => x.OwnerUserId, opt => opt.Ignore())
				.ForMember(x => x.Media, opt => opt.Ignore());

			CreateMap<Accessory, AccessoryResponseDTO>()
				.ForMember(a => a.BranchName,
					opt => opt.MapFrom(s => s.Branch.Name))
				.ForMember(a => a.BranchAddress,
					opt => opt.MapFrom(s => s.Branch.Address.District + "," + s.Branch.Address.Province))
				.ForMember(a => a.OwnerName,
					opt => opt.MapFrom(s => s.OwnerUser.FullName));
			CreateMap<UpdateAccessoryRequest, Accessory>()
				.ForMember(x => x.Id, opt => opt.Ignore())
				.ForMember(x => x.OwnerUserId, opt => opt.Ignore())
				.ForMember(x => x.Media, opt => opt.Ignore());

			CreateMap<Booking, BookingResponseDTO>()
			.ForMember(d => d.StatusText,
				opt => opt.MapFrom(s => s.Status.GetDisplayName()));
			CreateMap<BookingItem, BookingItemDTO>()
				// Map ItemId: ưu tiên Camera → Accessory → Combo
				.ForMember(d => d.ItemId,
					opt => opt.MapFrom(s => s.CameraId ?? s.AccessoryId ?? s.ComboId))

				// Map ItemName: ưu tiên theo loại nào có dữ liệu
				.ForMember(d => d.ItemName, opt => opt.MapFrom(s =>
					s.Camera != null
						? s.Camera.Brand + " " + s.Camera.Model
					: s.Accessory != null
						? s.Accessory.Brand + " " + s.Accessory.Model
					: s.Combo != null
						? s.Combo.Name
					: null
				))

				// Map loại item (nếu em có enum ItemType)
				.ForMember(d => d.ItemType, opt => opt.MapFrom(s =>
					s.CameraId != null
						? ItemType.Camera.ToString()
					: s.AccessoryId != null
						? ItemType.Accessory.ToString()
					: s.ComboId != null
						? ItemType.Combo.ToString()
					: null
				));

			CreateMap<Booking,Cart>();
			CreateMap<CreateBookingRequest, Booking>()
			// Set defaults for properties not in request
			.ForMember(dest => dest.Items, opt => opt.Ignore())
			.ForMember(dest => dest.Inspections, opt => opt.Ignore())
			.ForMember(dest => dest.RenterId, opt => opt.Ignore())
			.ForMember(dest => dest.StaffId, opt => opt.Ignore())
			.ForMember(dest => dest.BranchId, opt => opt.Ignore());

			CreateMap<Branch, BranchResponse>()
				.ForMember(b => b.ManagerName, 
				opt => opt.MapFrom(b => b.Manager.FullName));
			CreateMap<BranchRequest, Branch>();
			CreateMap<UserBranchMembership, BranchMembership>()
				.ForMember(bm => bm.FullName,
					opt => opt.MapFrom(ubm => ubm.User.FullName))
				.ForMember(bm => bm.Phone,
					opt => opt.MapFrom(ubm => ubm.User.Phone))
				.ForMember(bm => bm.Email,
					opt => opt.MapFrom(ubm => ubm.User.Email));

			CreateMap<FileAsset, FileAssetDTO>()
				.ForMember(d => d.Url,
					opt => opt.MapFrom(s => s.Url))
				.ForMember(d => d.ContentType,
					opt => opt.MapFrom(s => s.ContentType))
				.ForMember(d => d.SizeBytes,
					opt => opt.MapFrom(s => s.SizeBytes))
				.ForMember(d => d.Label,
					opt => opt.MapFrom(s => s.Label));
			CreateMap<VerificationRequest, VerificationResponseDTO>()
				.ForMember(d => d.StaffId,
					opt => opt.MapFrom(s => s.StaffId))
				.ForMember(d => d.StaffName,
					opt => opt.MapFrom(s => s.Staff.FullName))
				.ForMember(d => d.BranchName,
					opt => opt.MapFrom(s => s.Branch.Name))
				.ForMember(d => d.Address,
					opt => opt.MapFrom(s => s.Branch.Address.District + " " + s.Branch.Address.Province))
				.ForMember(d => d.Items,
					opt => opt.MapFrom(s => s.Items));
			// CreateVerificationRequestDTO -> VerificationRequest
			CreateMap<CreateVerificationRequestDTO, VerificationRequest>()
				.ForMember(d => d.Status, o => o.MapFrom(_ => "pending"))
				.ForMember(d => d.Owner, o => o.Ignore())
				.ForMember(d => d.Staff, o => o.Ignore())
				.ForMember(d => d.Inspections, o => o.Ignore());
			// DTO -> Entity
			CreateMap<VerificationItemDTO, VerificationRequestItem>()
				.ForMember(d => d.CameraId,
					o => o.MapFrom(s =>
						s.ItemType == ItemType.Camera ? s.ItemId : (Guid?)null))
				.ForMember(d => d.AccessoryId,
					o => o.MapFrom(s =>
						s.ItemType == ItemType.Accessory ? s.ItemId : (Guid?)null))
				.ForMember(d => d.VerificationId, o => o.Ignore())
				.ForMember(d => d.VerificationRequest, o => o.Ignore())
				.ForMember(d => d.Camera, o => o.Ignore())
				.ForMember(d => d.Accessory, o => o.Ignore());

			// Entity -> DTO
			CreateMap<VerificationRequestItem, VerificationItemDTO>()
				.ForMember(d => d.ItemId,
					o => o.MapFrom(s => s.CameraId ?? s.AccessoryId))
				.ForMember(d => d.ItemName,
					o => o.MapFrom(s =>
						s.Camera != null ? s.Camera.Brand + " " + s.Camera.Model : // tuỳ field
						s.Accessory != null ? s.Accessory.Brand + " " + s.Accessory.Model :
						null))
				.ForMember(d => d.ItemType,
					o => o.MapFrom(s =>
						s.CameraId != null ? ItemType.Camera :
						s.AccessoryId != null ? ItemType.Accessory :
						ItemType.Combo)); // fallback


			// DTO -> Entity
			CreateMap<InspectionRequest, Inspection>()
				// Map ItemId + ItemType -> CameraId / AccessoryId
				.ForMember(d => d.CameraId,
					opt => opt.MapFrom(s =>
						s.ItemType == ItemType.Camera ? s.ItemId : (Guid?)null))
				.ForMember(d => d.AccessoryId,
					opt => opt.MapFrom(s =>
						s.ItemType == ItemType.Accessory ? s.ItemId : (Guid?)null))

				// Map BookingId / VerificationId theo InspectionType
				.ForMember(d => d.BookingId,
					opt => opt.MapFrom(s =>
						s.Type == InspectionType.Booking ? s.InspectionTypeId : null))
				.ForMember(d => d.VerificationId,
					opt => opt.MapFrom(s =>
						s.Type == InspectionType.Verification ? s.InspectionTypeId : null))

				// Các navigation để Ignore, set ở service / EF
				.ForMember(d => d.Booking, opt => opt.Ignore())
				.ForMember(d => d.Verification, opt => opt.Ignore())
				.ForMember(d => d.Branch, opt => opt.Ignore())
				.ForMember(d => d.Staff, opt => opt.Ignore())
				.ForMember(d => d.Camera, opt => opt.Ignore())
				.ForMember(d => d.Accessory, opt => opt.Ignore());

			// Entity -> DTO
			CreateMap<Inspection, InspectionResponseDTO>()
				// ItemType: dựa theo CameraId / AccessoryId
				.ForMember(d => d.ItemType,
					opt => opt.MapFrom(s =>
						s.CameraId != null ? ItemType.Camera :
						s.AccessoryId != null ? ItemType.Accessory :
						ItemType.Camera)) // fallback, hoặc bạn cho DTO nullable cũng được

				// ItemName: lấy từ Camera / Accessory
				.ForMember(d => d.ItemName,
					opt => opt.MapFrom(s =>
						s.Camera != null ? s.Camera.Brand + " " + s.Camera.Model :      // đổi theo field bạn thích
						s.Accessory != null ? s.Accessory.Brand + " " + s.Accessory.Model :
						null))

				// Media: tuỳ bạn lấy từ FileAsset, tạm ignore trong mapping
				.ForMember(d => d.Media, opt => opt.Ignore());

			CreateMap<UpdateInspectionRequest, Inspection>()
				.ForMember(x => x.Id, opt => opt.Ignore());

			CreateMap<UpdateVerificationRequestDTO, VerificationRequest>()
				.ForMember(d => d.Items, o => o.Ignore())        // xử lý Items trong service
				.ForMember(d => d.Owner, o => o.Ignore())
				.ForMember(d => d.Staff, o => o.Ignore())
				.ForMember(d => d.Inspections, o => o.Ignore())
				// only map when source member is not null -> supports partial update
				.ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

			// Map signer
			CreateMap<ContractSignature, ContractSignerDTO>()
				.ForMember(d => d.FullName,
					opt => opt.MapFrom(s => s.User != null ? s.User.FullName : null));

			// Map contract
			CreateMap<Contract, ContractResponse>()
				.ForMember(d => d.BranchName,
					opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))
				.ForMember(d => d.BranchAddress,
					opt => opt.MapFrom(s => s.Branch != null && s.Branch.Address != null
						? s.Branch.Address.District + "," + s.Branch.Address.Province
						: null));
			CreateMap<Wallet, WalletSummaryResponse>();
			CreateMap<WalletTransaction, WalletTransactionResponse>();
		}
	}
}
