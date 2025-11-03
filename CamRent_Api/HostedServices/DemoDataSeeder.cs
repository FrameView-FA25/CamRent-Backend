using CamRent_Domain.Entities;
using CamRent_Domain.Common;
using CamRent_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CamRent_Api.HostedServices
{
	public class DemoDataSeeder : BackgroundService
	{
		private readonly IServiceProvider _provider;
		private readonly ILogger<DemoDataSeeder> _logger;
		public DemoDataSeeder(IServiceProvider provider, ILogger<DemoDataSeeder> logger)
		{
			_provider = provider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			using var scope = _provider.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<CamRentDbContext>();
			await db.Database.MigrateAsync(stoppingToken);

			if (await db.Cameras.AnyAsync(stoppingToken)) return;

			var branch = new Branch { Id = Guid.NewGuid(), Name = "Demo Branch", CreatedAt = DateTime.UtcNow };
			var owner = new User { Id = Guid.NewGuid(), Email = "owner@demo", FullName = "Owner Demo", CreatedAt = DateTime.UtcNow };
			var renter = new User { Id = Guid.NewGuid(), Email = "renter@demo", FullName = "Renter Demo", CreatedAt = DateTime.UtcNow };
			await db.Branches.AddAsync(branch, stoppingToken);
			await db.Users.AddRangeAsync(new[] { owner, renter }, stoppingToken);

			var cam = new Camera
			{
				Id = Guid.NewGuid(), Brand = "Canon", Model = "R5", BranchId = branch.Id,
				Ownership = OwnershipType.Owner, OwnerUserId = owner.Id,
				BaseDailyRate = 200_000m, PlatformFeePercent = 20m,
				EstimatedValueVnd = 30_000_000m, DepositPercent = 30m, DepositCapMinVnd = 5_000_000m, DepositCapMaxVnd = 30_000_000m,
				CreatedAt = DateTime.UtcNow
			};
			await db.Cameras.AddAsync(cam, stoppingToken);

			await db.SaveChangesAsync(stoppingToken);
			_logger.LogInformation("Demo data seeded");
		}
	}
}
