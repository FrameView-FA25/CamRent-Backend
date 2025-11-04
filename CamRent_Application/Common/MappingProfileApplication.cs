using AutoMapper;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using static CamRent_Application.DTOs.CameraDTO;

namespace CamRent_Application.Common
{
	public class MappingProfileApplication : Profile
	{
		public MappingProfileApplication()
		{
			CreateMap<Camera, CameraResponseDTO>();
		}
	}
}
