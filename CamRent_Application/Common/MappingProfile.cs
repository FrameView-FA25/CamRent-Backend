using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;

namespace CamRent_Application.Common
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Camera, CameraResponseDTO>();
		}
	}
}
