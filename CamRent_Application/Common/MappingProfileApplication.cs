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
				.ForMember(c => c.BranchName, opt => opt.MapFrom(s => s.Branch.Name))
				.ForMember(c => c.BranchAddress,
					opt => opt.MapFrom(s => s.Branch.Address.District + "," + s.Branch.Address.Province))
				.ForMember(c => c.OwnerName, opt => opt.MapFrom(s => s.OwnerUser.FullName));

			CreateMap<UpdateCameraRequest, Camera>()
				.ForMember(x => x.Id, opt => opt.Ignore())
				.ForMember(x => x.OwnerUserId, opt => opt.Ignore())
				.ForMember(x => x.Media, opt => opt.Ignore());

			CreateMap<Accessory, AccessoryResponseDTO>()
				.ForMember(a => a.BranchName, opt => opt.MapFrom(s => s.Branch.Name))
				.ForMember(a => a.BranchAddress,
					opt => opt.MapFrom(s => s.Branch.Address.District + "," + s.Branch.Address.Province))
				.ForMember(a => a.OwnerName, opt => opt.MapFrom(s => s.OwnerUser.FullName));

			CreateMap<UpdateAccessoryRequest, Accessory>()
				.ForMember(x => x.Id, opt => opt.Ignore())
				.ForMember(x => x.OwnerUserId, opt => opt.Ignore())
				.ForMember(x => x.Media, opt => opt.Ignore());

			CreateMap<Booking, BookingResponseDTO>()
				.ForMember(d => d.StatusText, opt => opt.MapFrom(s => s.Status.GetDisplayName()))
				.ForMember(d => d.StaffName, opt => opt.MapFrom(s => s.Staff != null ? s.Staff.FullName : null))
				.ForMember(d => d.BranchName, opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))
				.ForMember(d => d.BranchAddress,
					opt => opt.MapFrom(s => s.Branch.Address.District + "," + s.Branch.Address.Province));

			CreateMap<BookingItem, BookingItemDTO>()
				.ForMember(d => d.ItemId, opt => opt.MapFrom(s => s.CameraId ?? s.AccessoryId ?? s.ComboId))
				.ForMember(d => d.ItemName, opt => opt.MapFrom(s =>
					s.Camera != null
						? s.Camera.Brand + " " + s.Camera.Model
						: s.Accessory != null
							? s.Accessory.Brand + " " + s.Accessory.Model
							: s.Combo != null
								? s.Combo.Name
								: null
				))
				.ForMember(d => d.ItemType, opt => opt.MapFrom(s =>
					s.CameraId != null
						? ItemType.Camera.ToString()
						: s.AccessoryId != null
							? ItemType.Accessory.ToString()
							: s.ComboId != null
								? ItemType.Combo.ToString()
								: null
				));

			CreateMap<Booking, Cart>();

			CreateMap<CreateBookingRequest, Booking>()
				.ForMember(dest => dest.Items, opt => opt.Ignore())
				.ForMember(dest => dest.RenterId, opt => opt.Ignore())
				.ForMember(dest => dest.StaffId, opt => opt.Ignore())
				.ForMember(dest => dest.BranchId, opt => opt.Ignore());

			CreateMap<Branch, BranchResponse>()
				.ForMember(b => b.ManagerName, opt => opt.MapFrom(b => b.Manager.FullName));

			CreateMap<BranchRequest, Branch>();

			CreateMap<UserBranchMembership, BranchMembership>()
				.ForMember(bm => bm.FullName, opt => opt.MapFrom(ubm => ubm.User.FullName))
				.ForMember(bm => bm.Phone, opt => opt.MapFrom(ubm => ubm.User.Phone))
				.ForMember(bm => bm.Email, opt => opt.MapFrom(ubm => ubm.User.Email));

			CreateMap<FileAsset, FileAssetDTO>()
				.ForMember(d => d.Url, opt => opt.MapFrom(s => s.Url))
				.ForMember(d => d.ContentType, opt => opt.MapFrom(s => s.ContentType))
				.ForMember(d => d.SizeBytes, opt => opt.MapFrom(s => s.SizeBytes))
				.ForMember(d => d.Label, opt => opt.MapFrom(s => s.Label));

			CreateMap<VerificationRequest, VerificationResponseDTO>()
				.ForMember(d => d.StaffId, opt => opt.MapFrom(s => s.StaffId))
				.ForMember(d => d.StaffName, opt => opt.MapFrom(s => s.Staff.FullName))
				.ForMember(d => d.BranchName, opt => opt.MapFrom(s => s.Branch.Name))
				.ForMember(d => d.Address,
					opt => opt.MapFrom(s => s.Branch.Address.District + " " + s.Branch.Address.Province))
				.ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

			CreateMap<CreateVerificationRequestDTO, VerificationRequest>()
				.ForMember(d => d.Status, o => o.MapFrom(_ => "pending"))
				.ForMember(d => d.Owner, o => o.Ignore())
				.ForMember(d => d.Staff, o => o.Ignore());

			CreateMap<VerificationItemDTO, VerificationRequestItem>()
				.ForMember(d => d.CameraId,
					o => o.MapFrom(s => s.ItemType == ItemType.Camera ? s.ItemId : (Guid?)null))
				.ForMember(d => d.AccessoryId,
					o => o.MapFrom(s => s.ItemType == ItemType.Accessory ? s.ItemId : (Guid?)null))
				.ForMember(d => d.VerificationId, o => o.Ignore())
				.ForMember(d => d.VerificationRequest, o => o.Ignore())
				.ForMember(d => d.Camera, o => o.Ignore())
				.ForMember(d => d.Accessory, o => o.Ignore());

			CreateMap<VerificationRequestItem, VerificationItemDTO>()
				.ForMember(d => d.ItemId, o => o.MapFrom(s => s.CameraId ?? s.AccessoryId))
				.ForMember(d => d.ItemName, o => o.MapFrom(s =>
					s.Camera != null ? s.Camera.Brand + " " + s.Camera.Model :
					s.Accessory != null ? s.Accessory.Brand + " " + s.Accessory.Model :
					null))
				.ForMember(d => d.ItemType, o => o.MapFrom(s =>
					s.CameraId != null ? ItemType.Camera :
					s.AccessoryId != null ? ItemType.Accessory :
					ItemType.Combo));

			CreateMap<Inspection, InspectionResponseDTO>()
				.ForMember(d => d.ItemName, opt => opt.Ignore())
				.ForMember(d => d.ItemType, opt => opt.Ignore())
				.ForMember(d => d.Media, opt => opt.Ignore())
				.ForMember(d => d.Methods, opt => opt.Ignore());

			CreateMap<UpdateVerificationRequestDTO, VerificationRequest>()
				.ForMember(d => d.Items, o => o.Ignore())
				.ForMember(d => d.Owner, o => o.Ignore())
				.ForMember(d => d.Staff, o => o.Ignore())
				.ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

			CreateMap<ContractSignature, ContractSignerDTO>()
				.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.User != null ? s.User.FullName : null));

			CreateMap<Contract, ContractResponse>()
				.ForMember(d => d.BranchName, opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))
				.ForMember(d => d.BranchAddress,
					opt => opt.MapFrom(s => s.Branch != null && s.Branch.Address != null
						? s.Branch.Address.District + "," + s.Branch.Address.Province
						: null));

			CreateMap<Wallet, WalletSummaryResponse>();
			CreateMap<WalletTransaction, WalletTransactionResponse>();
			CreateMap<Payment, PaymentDTO>()
			.ForMember(d => d.Lines, opt => opt.MapFrom(s => s.Lines));
			CreateMap<PaymentLine, PaymentLineDTO>();
		}
	}
}

