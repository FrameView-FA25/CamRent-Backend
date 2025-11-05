using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CamRent_Infrastructure.Persistence.SeedData
{
	public class DemoDataSeeder
	{
		private readonly CamRentDbContext _db;
		private readonly ILogger<DemoDataSeeder> _logger;

		public DemoDataSeeder(CamRentDbContext db, ILogger<DemoDataSeeder> logger)
		{
			_db = db;
			_logger = logger;
		}

		public async Task RunAsync(CancellationToken ct = default)
		{
			if (!"true".Equals(Environment.GetEnvironmentVariable("SEED_DEMO"), StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation("SEED_DEMO != true => skip seeding");
				return;
			}

			const string SeedKey = "demo-v1";
			if (await _db.SeedHistories.AsNoTracking().AnyAsync(x => x.Key == SeedKey, ct))
			{
				_logger.LogInformation("Seed {SeedKey} already applied.", SeedKey);
				return;
			}

			// All-or-nothing
			await using var tx = await _db.Database.BeginTransactionAsync(ct);
			try
			{
				var rng = new Random(1234);

				var branches = new List<Branch>
			{
				new Branch { Id = Guid.NewGuid(), Name = "Branch District 1", CreatedAt = DateTime.UtcNow },
				new Branch { Id = Guid.NewGuid(), Name = "Branch District 7", CreatedAt = DateTime.UtcNow },
				new Branch { Id = Guid.NewGuid(), Name = "Branch Thu Duc", CreatedAt = DateTime.UtcNow }
			};
				await _db.Branches.AddRangeAsync(branches, ct);

				var owners = Enumerable.Range(1, 5).Select(i => new User { Id = Guid.NewGuid(), Email = $"owner{i}@demo", FullName = $"Owner {i}", CreatedAt = DateTime.UtcNow }).ToList();
				var renters = Enumerable.Range(1, 5).Select(i => new User { Id = Guid.NewGuid(), Email = $"renter{i}@demo", FullName = $"Renter {i}", CreatedAt = DateTime.UtcNow }).ToList();
				var managers = branches.Select((b, idx) => new User { Id = Guid.NewGuid(), Email = $"manager{idx + 1}@demo", FullName = $"Manager {idx + 1}", CreatedAt = DateTime.UtcNow }).ToList();
				var deliveries = branches.Select((b, idx) => new User { Id = Guid.NewGuid(), Email = $"delivery{idx + 1}@demo", FullName = $"Delivery {idx + 1}", CreatedAt = DateTime.UtcNow }).ToList();
				await _db.Users.AddRangeAsync(owners.Concat(renters).Concat(managers).Concat(deliveries).ToList(), ct);

				var roleMappings = new List<UserRoleMapping>();
				roleMappings.AddRange(owners.Select(u => new UserRoleMapping { Id = Guid.NewGuid(), UserId = u.Id, Role = UserRole.Owner, CreatedAt = DateTime.UtcNow }));
				roleMappings.AddRange(renters.Select(u => new UserRoleMapping { Id = Guid.NewGuid(), UserId = u.Id, Role = UserRole.Renter, CreatedAt = DateTime.UtcNow }));
				roleMappings.AddRange(managers.Select(u => new UserRoleMapping { Id = Guid.NewGuid(), UserId = u.Id, Role = UserRole.BranchManager, CreatedAt = DateTime.UtcNow }));
				roleMappings.AddRange(deliveries.Select(u => new UserRoleMapping { Id = Guid.NewGuid(), UserId = u.Id, Role = UserRole.Staff, CreatedAt = DateTime.UtcNow }));
				await _db.UserRoles.AddRangeAsync(roleMappings, ct);

				var memberships = new List<UserBranchMembership>();
				foreach (var m in managers)
					memberships.Add(new UserBranchMembership { Id = Guid.NewGuid(), UserId = m.Id, BranchId = branches[rng.Next(branches.Count)].Id, CreatedAt = DateTime.UtcNow });
				foreach (var d in deliveries)
					memberships.Add(new UserBranchMembership { Id = Guid.NewGuid(), UserId = d.Id, BranchId = branches[rng.Next(branches.Count)].Id, CreatedAt = DateTime.UtcNow });
				await _db.BranchMemberships.AddRangeAsync(memberships, ct);

				var categories = new List<Category>
			{
				new Category { Id = Guid.NewGuid(), Name = "Camera",   CreatedAt = DateTime.UtcNow },
				new Category { Id = Guid.NewGuid(), Name = "Lens",     CreatedAt = DateTime.UtcNow },
				new Category { Id = Guid.NewGuid(), Name = "Accessory",CreatedAt = DateTime.UtcNow }
			};
				await _db.Categories.AddRangeAsync(categories, ct);

				var cameraModels = new (string brand, string model, decimal value)[]
				{
				("Canon","R5", 30_000_000m), ("Canon","R6", 25_000_000m), ("Canon","EOS 90D", 18_000_000m),
				("Sony","A7III", 28_000_000m), ("Sony","A7IV", 35_000_000m), ("Sony","A6400", 16_000_000m),
				("Nikon","Z6", 26_000_000m), ("Nikon","Z7", 38_000_000m), ("Nikon","D750", 20_000_000m),
				("Fujifilm","X-T4", 22_000_000m), ("Fujifilm","X-T5", 32_000_000m), ("Panasonic","S5", 27_000_000m)
				};
				var cameras = new List<Camera>();
				foreach (var (brand, model, value) in cameraModels)
				{
					var owner = owners[rng.Next(owners.Count)];
					var branch = branches[rng.Next(branches.Count)];
					cameras.Add(new Camera
					{
						Id = Guid.NewGuid(),
						Brand = brand,
						Model = model,
						BranchId = branch.Id,
						Ownership = OwnershipType.Owner,
						OwnerUserId = owner.Id,
						BaseDailyRate = Math.Round(value * 0.008m, 0),
						PlatformFeePercent = rng.Next(15, 26),
						EstimatedValueVnd = value,
						DepositPercent = rng.Next(20, 41),
						DepositCapMinVnd = 5_000_000m,
						DepositCapMaxVnd = 30_000_000m,
						CreatedAt = DateTime.UtcNow
					});
				}
				await _db.Cameras.AddRangeAsync(cameras, ct);

				var accessoryModels = new (string brand, string model, decimal value)[]
				{
				("Canon","RF 24-70", 25_000_000m), ("Sony","FE 70-200", 40_000_000m), ("Nikon","Z 50", 8_000_000m),
				("Sigma","18-35", 12_000_000m), ("Tamron","28-75", 10_000_000m), ("DJI","RS3", 15_000_000m),
				("Rode","VideoMic", 3_000_000m), ("Manfrotto","Tripod 190", 4_000_000m), ("Godox","SL60W", 3_500_000m),
				("Sandisk","Extreme 128GB", 1_000_000m), ("Sony","NP-FZ100", 1_200_000m), ("Fujifilm","XF 56", 18_000_000m)
				};
				var accessories = new List<Accessory>();
				foreach (var (brand, model, value) in accessoryModels)
				{
					var owner = owners[rng.Next(owners.Count)];
					var branch = branches[rng.Next(branches.Count)];
					accessories.Add(new Accessory
					{
						Id = Guid.NewGuid(),
						Brand = brand,
						Model = model,
						BranchId = branch.Id,
						Ownership = OwnershipType.Owner,
						OwnerUserId = owner.Id,
						BaseDailyRate = Math.Round(value * 0.006m, 0),
						PlatformFeePercent = rng.Next(15, 26),
						EstimatedValueVnd = value,
						DepositPercent = rng.Next(20, 41),
						DepositCapMinVnd = 5_000_000m,
						DepositCapMaxVnd = 30_000_000m,
						CreatedAt = DateTime.UtcNow
					});
				}
				await _db.Accessories.AddRangeAsync(accessories, ct);

				var catCamera = categories.First(c => c.Name == "Camera");
				var catLens = categories.First(c => c.Name == "Lens");
				var catAccessory = categories.First(c => c.Name == "Accessory");

				var links = new List<DeviceCategoryLink>();
				links.AddRange(cameras.Select(cam => new DeviceCategoryLink { Id = Guid.NewGuid(), CameraId = cam.Id, CategoryId = catCamera.Id, CreatedAt = DateTime.UtcNow }));
				links.AddRange(accessories.Select(acc => new DeviceCategoryLink
				{
					Id = Guid.NewGuid(),
					AccessoryId = acc.Id,
					CategoryId = (acc.Model.Contains("RF") || acc.Model.Contains("FE") || acc.Model.Contains("XF")) ? catLens.Id : catAccessory.Id,
					CreatedAt = DateTime.UtcNow
				}));
				await _db.DeviceCategories.AddRangeAsync(links, ct);

				var combos = Enumerable.Range(1, 3)
					.Select(i => new Combo { Id = Guid.NewGuid(), Name = $"Combo {i}", Description = "Camera + Lens + Accessory", CreatedAt = DateTime.UtcNow })
					.ToList();
				await _db.Combos.AddRangeAsync(combos, ct);

				var comboItems = new List<ComboItem>();
				foreach (var combo in combos)
				{
					var cam = cameras[rng.Next(cameras.Count)];
					var acc1 = accessories[rng.Next(accessories.Count)];
					var acc2 = accessories[rng.Next(accessories.Count)];
					comboItems.Add(new ComboItem { Id = Guid.NewGuid(), ComboId = combo.Id, CameraId = cam.Id, Quantity = 1, CreatedAt = DateTime.UtcNow });
					comboItems.Add(new ComboItem { Id = Guid.NewGuid(), ComboId = combo.Id, AccessoryId = acc1.Id, Quantity = 1, CreatedAt = DateTime.UtcNow });
					comboItems.Add(new ComboItem { Id = Guid.NewGuid(), ComboId = combo.Id, AccessoryId = acc2.Id, Quantity = 1, CreatedAt = DateTime.UtcNow });
				}
				await _db.ComboItems.AddRangeAsync(comboItems, ct);

				_db.SeedHistories.Add(new SeedHistory { Key = SeedKey });
				await _db.SaveChangesAsync(ct);
				await tx.CommitAsync(ct);

				_logger.LogInformation("Demo data seeded xong.");
			}
			catch (Exception ex)
			{
				await tx.RollbackAsync(ct);
				_logger.LogError(ex, "Seeding failed");
				throw; // (tuỳ chọn) để biết deploy fail sớm
			}
		}
	}

}
