using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Identity;
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
				// Create demo users (email: [role]@gmail.com, password: 12345)
				var rolesToSeed = new (string Prefix, UserRole Role)[]
				{
					("admin", UserRole.Admin),
					("manager", UserRole.BranchManager), // "Manager" maps to BranchManager enum
					("staff", UserRole.Staff),
					("renter", UserRole.Renter),
					("owner", UserRole.Owner)
				};

				var hasher = new PasswordHasher<User>();
				foreach (var (prefix, role) in rolesToSeed)
				{
					var email = $"{prefix}@gmail.com";
					// skip if already exists (defensive)
					if (await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct))
						continue;

					var user = new User
					{
						Id = Guid.NewGuid(),
						Email = email,
						NormalizedEmail = email.ToUpperInvariant(),
						Phone = "0123456789",
						FullName = prefix.FirstCharToUpper(), // FullName = role name (e.g., "Admin", "Manager", ...)
						Status = UserStatus.Active,
						CreatedAt = DateTime.UtcNow
					};

					user.PasswordHash = hasher.HashPassword(user, "123456");

					_db.Users.Add(user);
					_db.UserRoles.Add(new UserRoleMapping
					{
						Id = Guid.NewGuid(),
						User = user,
						Role = role,
						CreatedAt = DateTime.UtcNow
					});
				}

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

	static class StringExtensions
	{
		public static string FirstCharToUpper(this string input)
		{
			if (string.IsNullOrEmpty(input)) return input;
			if (input.Length == 1) return input.ToUpperInvariant();
			return char.ToUpperInvariant(input[0]) + input.Substring(1);
		}
	}
}
