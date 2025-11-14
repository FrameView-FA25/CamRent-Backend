using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.AccessoryDTO;
using static CamRent_Application.DTOs.BookingDTO;
using static CamRent_Application.DTOs.BranchDTO;
using static CamRent_Application.DTOs.CameraDTO;
using static CamRent_Application.DTOs.VerificationRequestDTO;

namespace CamRent_Application.Common
{
	public class MappingProfileApplication : Profile
	{
		public MappingProfileApplication()
		{
			CreateMap<Camera, CameraResponseDTO>()
				.ForMember(c => c.BranchName,
					opt => opt.MapFrom(s => s.Branch.Name));
			CreateMap<Accessory, AccessoryResponseDTO>()
				.ForMember(a => a.BranchName,
					opt => opt.MapFrom(s => s.Branch.Name));
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

				// Map loại item (nếu em có enum BookingItemType)
				.ForMember(d => d.ItemType, opt => opt.MapFrom(s =>
					s.CameraId != null
						? BookingItemType.Camera.ToString()
					: s.AccessoryId != null
						? BookingItemType.Accessory.ToString()
					: s.ComboId != null
						? BookingItemType.Combo.ToString()
					: null
				)); ;
			CreateMap<Booking,Cart>();
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
					opt => opt.MapFrom(s => s.Branch.Address.District + " " + s.Branch.Address.Province));
			CreateMap<CreateVerificationRequestDTO, VerificationRequest>();
		}
	}
}
