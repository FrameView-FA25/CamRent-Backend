using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CamRent_Infrastructure.Persistence
{
	public class CamRentDbContextFactory : IDesignTimeDbContextFactory<CamRentDbContext>
	{
		public CamRentDbContext CreateDbContext(string[] args)
		{
			// Thư mục API để lấy appsettings
			var basePath = Directory.GetCurrentDirectory();
			var apiPath = Path.Combine(basePath, "..", "CamRent_Api");

			var config = new ConfigurationBuilder()
				.AddJsonFile(Path.Combine(apiPath, "appsettings.json"), optional: false)
				.AddJsonFile(Path.Combine(apiPath, "appsettings.Development.json"), optional: true)
				.AddEnvironmentVariables() // chỉ bật nếu đã cài package EnvironmentVariables
				.Build();

			var connStr = config.GetConnectionString("Default");

			var options = new DbContextOptionsBuilder<CamRentDbContext>()
				.UseNpgsql(connStr, b => b.MigrationsAssembly("CamRent_Infrastructure"))
				.Options;

			return new CamRentDbContext(options);
		}
	}
}
