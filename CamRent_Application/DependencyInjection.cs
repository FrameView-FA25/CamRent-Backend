using CamRent_Application.IServices;
using CamRent_Application.Services;
using CamRent_Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CamRent_Application
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApplicationDI(this IServiceCollection services)
		{
			services.AddScoped<ICameraService, CameraService>();
			return services;
		}
	}
}
