using AutoMapper;
using CamRent_Domain.Entities;
using static CamRent_Api.Models.AccessoryModel;
using static CamRent_Api.Models.BookingModel;
using static CamRent_Api.Models.CameraModel;

namespace CamRent_Api.Commons
{
	public class MappingProfileApi : Profile
	{
		public MappingProfileApi()
		{
			CreateMap<CameraRequest, Camera>()
				.ForMember(dest => dest.Media, opt => opt.Ignore());
			CreateMap<AccessoryRequest, Accessory>()
				.ForMember(d => d.OwnerUserId, opt => opt.Ignore());
		}
	}
}
