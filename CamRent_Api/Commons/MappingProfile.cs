using AutoMapper;
using CamRent_Domain.Entities;
using static CamRent_Api.Models.CameraModel;

namespace CamRent_Api.Commons
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<CreateCameraRequest, Camera>();
		}
	}
}
