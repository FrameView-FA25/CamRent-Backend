using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using CamRent_Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Infrastructure.Persistence
{
    public class CamRentDbContext : DbContext
    {
        public CamRentDbContext(DbContextOptions<CamRentDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserRoleMapping> UserRoles => Set<UserRoleMapping>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<UserBranchMembership> BranchMemberships => Set<UserBranchMembership>();
        public DbSet<FileAsset> Files => Set<FileAsset>();

        public DbSet<Camera> Cameras => Set<Camera>();
        public DbSet<Accessory> Accessories => Set<Accessory>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingItem> BookingItems => Set<BookingItem>();
        public DbSet<Inspection> Inspections => Set<Inspection>();
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<ContractSignature> ContractSignatures => Set<ContractSignature>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentLine> PaymentLines => Set<PaymentLine>();
        public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
        public DbSet<Dispute> Disputes => Set<Dispute>();
        public DbSet<DisputeItem> DisputeItems => Set<DisputeItem>();

        public DbSet<Combo> Combos => Set<Combo>();
        public DbSet<ComboItem> ComboItems => Set<ComboItem>();
        public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
        public DbSet<VerificationRequestItem> VerificationRequestItems => Set<VerificationRequestItem>();
        public DbSet<SeedHistory> SeedHistories => Set<SeedHistory>();
        public DbSet<ResetPasswordToken> ResetPasswordTokens => Set<ResetPasswordToken>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
        public DbSet<HandoverReceipt> HandoverReceipts => Set<HandoverReceipt>();
        public DbSet<MoneyFlatformSetting> MoneyFlatformSettings => Set<MoneyFlatformSetting>();
		public DbSet<WorkSlotDefinition> WorkSlotDefinitions => Set<WorkSlotDefinition>();
		public DbSet<Review> Reviews => Set<Review>();
		public DbSet<HomePageCarouselItem> HomePageCarouselItems => Set<HomePageCarouselItem>();
		public DbSet<HomePageBlock> HomePageBlocks => Set<HomePageBlock>();
		public DbSet<BookingIssueReport> BookingIssueReports => Set<BookingIssueReport>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
			
			// snake_case convention
			foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.GetColumnName()));
                }
            }


            // Store enums as their string names in the database (e.g. "Confirmed" / "Active" / "Pending")
            foreach (var et in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var prop in et.GetProperties())
                {
                    var clrType = prop.ClrType;
                    Type? enumType = null;

                    if (clrType.IsEnum)
                        enumType = clrType;
                    else
                    {
                        var underlying = Nullable.GetUnderlyingType(clrType);
                        if (underlying != null && underlying.IsEnum)
                            enumType = underlying;
                    }

                    if (enumType != null)
                    {
                        var converterType = typeof(EnumToStringConverter<>).MakeGenericType(enumType);
                        var converter = Activator.CreateInstance(converterType) as ValueConverter;
                        if (converter != null)
                            prop.SetValueConverter(converter);
                    }
                }
            }

			modelBuilder.Owned<Address>();

			modelBuilder.Entity<SeedHistory>(e =>
			{
				e.ToTable("SeedHistories");
				e.HasKey(x => x.Id);
				e.Property(x => x.Key).HasMaxLength(100).IsRequired();
				e.HasIndex(x => x.Key).IsUnique();
			});

            modelBuilder.Entity<User>()
                .HasOne(u => u.SignatureAsset)
                .WithMany()
                .HasForeignKey(u => u.SignatureAssetId);
            modelBuilder.Entity<User>()
                .HasOne(u => u.Avatar)
                .WithMany()
                .HasForeignKey(u => u.AvatarId);
			// Relationships
			modelBuilder.Entity<UserBranchMembership>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId);
            modelBuilder.Entity<UserBranchMembership>()
                .HasOne(m => m.Branch)
                .WithMany(b => b.UserMemberships)
                .HasForeignKey(m => m.BranchId);
            modelBuilder.Entity<Branch>()
                .HasOne(b => b.Manager)
                .WithMany()
                .HasForeignKey(b => b.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
			modelBuilder.Entity<UserRoleMapping>()
                .HasOne(m => m.User)
                .WithMany(u => u.Roles)
                .HasForeignKey(m => m.UserId);

            modelBuilder.Entity<Camera>()
                .HasOne(c => c.Branch)
                .WithMany()
                .HasForeignKey(c => c.BranchId);

            modelBuilder.Entity<Accessory>()
                .HasOne(a => a.Branch)
                .WithMany()
                .HasForeignKey(a => a.BranchId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Renter)
                .WithMany()
                .HasForeignKey(b => b.RenterId);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Staff)
                .WithMany()
                .HasForeignKey(b => b.StaffId);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Branch)
                .WithMany()
                .HasForeignKey(b => b.BranchId);

			modelBuilder.Entity<BookingIssueReport>()
				.HasOne(r => r.Booking)
				.WithMany()
				.HasForeignKey(r => r.BookingId);
			modelBuilder.Entity<BookingIssueReport>()
				.HasOne(r => r.ReporterUser)
				.WithMany()
				.HasForeignKey(r => r.ReporterUserId);
			modelBuilder.Entity<BookingIssueReport>()
				.HasOne(r => r.HandledByStaff)
				.WithMany()
				.HasForeignKey(r => r.HandledByStaffId)
				.OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Booking)
                .WithMany(b => b.Contracts)
                .HasForeignKey(c => c.BookingId);
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Verification)
                .WithMany(v => v.Contracts)
                .HasForeignKey(c => c.VerificationId);
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.FileAsset)
                .WithMany()
                .HasForeignKey(c => c.FileAssetId);
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Branch)
                .WithMany()
                .HasForeignKey(c => c.BranchId);

            modelBuilder.Entity<ContractSignature>()
                .HasOne(s => s.Contract)
                .WithMany(c => c.Signatures)
                .HasForeignKey(s => s.ContractId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId);

            modelBuilder.Entity<PaymentLine>()
                .HasOne(l => l.Payment)
                .WithMany(p => p.Lines)
                .HasForeignKey(l => l.PaymentId);

            modelBuilder.Entity<PaymentEvent>(e =>
            {
                e.HasIndex(x => x.RequestHash).IsUnique();
            });

            modelBuilder.Entity<DisputeItem>()
                .HasOne(i => i.Dispute)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.DisputeId);


            modelBuilder.Entity<ComboItem>()
                .HasOne(ci => ci.Combo)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.ComboId);

            modelBuilder.Entity<ComboItem>()
                .HasOne(ci => ci.Camera)
                .WithMany()
                .HasForeignKey(ci => ci.CameraId);

            modelBuilder.Entity<ComboItem>()
                .HasOne(ci => ci.Accessory)
                .WithMany()
                .HasForeignKey(ci => ci.AccessoryId);

            modelBuilder.Entity<BookingItem>()
                .HasOne(bi => bi.Booking)
                .WithMany(b => b.Items)
                .HasForeignKey(bi => bi.BookingId);

            modelBuilder.Entity<BookingItem>()
                .HasOne(bi => bi.Camera)
                .WithMany()
                .HasForeignKey(bi => bi.CameraId);

            modelBuilder.Entity<BookingItem>()
                .HasOne(bi => bi.Accessory)
                .WithMany()
                .HasForeignKey(bi => bi.AccessoryId);

            modelBuilder.Entity<BookingItem>()
                .HasOne(bi => bi.Combo)
                .WithMany()
                .HasForeignKey(bi => bi.ComboId);
            modelBuilder.Entity<ResetPasswordToken>()
	            .HasOne(t => t.User)
	            .WithMany()
	            .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<VerificationRequest>()
                         .HasOne(v => v.Staff)
                         .WithMany()
                         .HasForeignKey(v => v.StaffId);
            modelBuilder.Entity<VerificationRequest>()
                .HasOne(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.CreatedByUserId);
            modelBuilder.Entity<VerificationRequest>()
                         .HasOne(v => v.Branch)
                         .WithMany()
                         .HasForeignKey(v => v.BranchId);

            modelBuilder.Entity<VerificationRequestItem>()
                .HasOne(vi => vi.VerificationRequest)
                .WithMany(v => v.Items)
                .HasForeignKey(vi => vi.VerificationId);
            modelBuilder.Entity<VerificationRequestItem>()
                .HasOne(vi => vi.Camera)
                .WithMany()
                .HasForeignKey(vi => vi.CameraId);
            modelBuilder.Entity<VerificationRequestItem>()
                .HasOne(vi => vi.Accessory)
                .WithMany()
                .HasForeignKey(vi => vi.AccessoryId);

            modelBuilder.Entity<Inspection>()
                         .HasOne(i => i.Booking)
                         .WithMany(b => b.Inspections)
                         .HasForeignKey(i => i.BookingId);

            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Verification)
                .WithMany(v => v.Inspections)
                .HasForeignKey(i => i.VerificationId);

            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Branch)
                .WithMany()
                .HasForeignKey(i => i.BranchId);
            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Staff)
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserId);
            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Camera)
                .WithMany()
                .HasForeignKey(i => i.CameraId);
            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Accessory)
                .WithMany()
                .HasForeignKey(i => i.AccessoryId);

			modelBuilder.Entity<HomePageCarouselItem>()
				.HasOne(x => x.ImageAsset)
				.WithMany()
				.HasForeignKey(x => x.ImageAssetId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<HomePageBlock>()
				.HasOne(x => x.ImageAsset)
				.WithMany()
				.HasForeignKey(x => x.ImageAssetId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<HomePageBlock>()
				.HasIndex(x => x.Key)
				.IsUnique();

            modelBuilder.Entity<HandoverReceipt>()
                         .HasOne(hr => hr.Contract)
                         .WithMany()
                         .HasForeignKey(hr => hr.ContractId);
            modelBuilder.Entity<HandoverReceipt>()
                .HasOne(hr => hr.User)
                .WithMany()
                .HasForeignKey(hr => hr.UserId);
            modelBuilder.Entity<HandoverReceipt>()
                .HasOne(hr => hr.Staff)
                .WithMany()
                .HasForeignKey(hr => hr.CreatedByUserId);
            modelBuilder.Entity<HandoverReceipt>()
                .HasOne(hr => hr.Branch)
                .WithMany()
                .HasForeignKey(hr => hr.BranchId);
            modelBuilder.Entity<HandoverReceipt>()
                .HasOne(hr => hr.Inspection)
                .WithMany()
                .HasForeignKey(hr => hr.InspectionId);
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId);
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(wt => wt.WalletId);
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Payment)
                .WithMany()
                .HasForeignKey(wt => wt.PaymentId);
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Booking)
                .WithMany()
                .HasForeignKey(wt => wt.BookingId);

			//modelBuilder.Entity<DeliveryTask>()
			//             .HasOne(t => t.Booking)
			//             .WithMany()
			//             .HasForeignKey(t => t.BookingId);
			//modelBuilder.Entity<DeviceCategoryLink>()
			//    .HasOne(dc => dc.Camera)
			//    .WithMany(c => c.Categories)
			//    .HasForeignKey(dc => dc.CameraId);

			//modelBuilder.Entity<DeviceCategoryLink>()
			//    .HasOne(dc => dc.Accessory)
			//    .WithMany(a => a.Categories)
			//    .HasForeignKey(dc => dc.AccessoryId);

			//modelBuilder.Entity<DeviceCategoryLink>()
			//    .HasOne(dc => dc.Category)
			//    .WithMany()
			//    .HasForeignKey(dc => dc.CategoryId);
		}

		private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            var chars = new List<char>(name.Length + 10);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) chars.Add('_');
                    chars.Add(char.ToLowerInvariant(c));
                }
                else
                {
                    chars.Add(c);
                }
            }
            return new string(chars.ToArray());
        }

		// Bỏ VietnamOffset + VietnamNow đi, thay bằng UtcNow:
		private static DateTime UtcNow() => DateTime.UtcNow;

		public override int SaveChanges()
		{
			UpdateTimestamps();
			return base.SaveChanges();
		}

		public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			UpdateTimestamps();
			return base.SaveChangesAsync(cancellationToken);
		}

		private void UpdateTimestamps()
		{
			var now = UtcNow(); // DateTime (UTC)

			foreach (var entry in ChangeTracker.Entries<BaseEntity>())
			{
				if (entry.State == EntityState.Added)
				{
					if (entry.Property(nameof(BaseEntity.CreatedAt)) != null)
						entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue = now;
					if (entry.Property(nameof(BaseEntity.UpdatedAt)) != null)
						entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = now;
				}
				else if (entry.State == EntityState.Modified)
				{
					if (entry.Property(nameof(BaseEntity.UpdatedAt)) != null)
						entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = now;
				}
			}
		}
	}
}

