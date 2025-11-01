using CamRent_Application.Interfaces;
using CamRent_Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CamRent_Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructureDI(this IServiceCollection services)
		{
			services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			services.AddScoped<IUnitOfWork, UnitOfWork>();
			return services;
		}
	}
}
