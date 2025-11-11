using AutoMapper;
using CamRent_Domain.Entities;
using static CamRent_Api.Models.AccessoryModel;
using static CamRent_Api.Models.CameraModel;

namespace CamRent_Api.Commons
{
	public class MappingProfileApi : Profile
	{
		public MappingProfileApi()
		{
			CreateMap<CameraRequest, Camera>()
				 .ForMember(d => d.Media, o => o.Ignore()); ;
			CreateMap<AccessoryRequest, Accessory>()
			.ForMember(d => d.OwnerUserId, opt => opt.Ignore());
		}
	}
}
