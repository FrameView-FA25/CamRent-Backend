using CamRent_Application.Interfaces;
using CamRent_Infrastructure.Data;
using CamRent_Infrastructure.Persistence;
using CamRent_Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CamRent_Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructureDI(this IServiceCollection services, IConfiguration config)
		{
			services.AddDbContext<CamRentDbContext>(options =>options.UseNpgsql(config.GetConnectionString("Default")));

			services.AddScoped<DemoDataSeeder>();

			services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			services.AddScoped<IUnitOfWork, UnitOfWork>();
			return services;
		}
	}
}
