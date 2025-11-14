using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.AccessoryDTO;
using static CamRent_Application.DTOs.BookingDTO;
using static CamRent_Application.DTOs.BranchDTO;
using static CamRent_Application.DTOs.CameraDTO;

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
				.ForMember(d => d.CameraName,
					opt => opt.MapFrom(s => s.Camera.Brand + " " + s.Camera.Model))
				.ForMember(d => d.AccessoryName,
					opt => opt.MapFrom(s => s.Accessory.Brand + " " + s.Accessory.Model))
				.ForMember(d => d.ComboName,
					opt => opt.MapFrom(s => s.Combo.Name));
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
		}
	}
}
