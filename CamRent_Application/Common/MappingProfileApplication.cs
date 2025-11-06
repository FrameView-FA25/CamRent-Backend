using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.AccessoryDTO;
using static CamRent_Application.DTOs.BookingDTO;
using static CamRent_Application.DTOs.CameraDTO;

namespace CamRent_Application.Common
{
	public class MappingProfileApplication : Profile
	{
		public MappingProfileApplication()
		{
			CreateMap<Camera, CameraResponseDTO>();
			CreateMap<Accessory, AccessoryResponseDTO>();
			CreateMap<Booking, BookingResponseDTO>()
			.ForMember(d => d.StatusText,
				opt => opt.MapFrom(s => s.Status.GetDisplayName()));
			CreateMap<BookingItem, BookingItemDTO>();
		}
	}
}
