using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using CamRent_Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<Inspection> Inspections => Set<Inspection>();
        public DbSet<ContractTemplate> ContractTemplates => Set<ContractTemplate>();
        public DbSet<ContractInstance> Contracts => Set<ContractInstance>();
        public DbSet<ContractSigner> ContractSigners => Set<ContractSigner>();
        public DbSet<ContractEvent> ContractEvents => Set<ContractEvent>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<DeliveryTask> DeliveryTasks => Set<DeliveryTask>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentLine> PaymentLines => Set<PaymentLine>();
        public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
        public DbSet<Dispute> Disputes => Set<Dispute>();
        public DbSet<DisputeItem> DisputeItems => Set<DisputeItem>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<DeviceCategoryLink> DeviceCategories => Set<DeviceCategoryLink>();
        public DbSet<Combo> Combos => Set<Combo>();
        public DbSet<ComboItem> ComboItems => Set<ComboItem>();
        public DbSet<BookingItem> BookingItems => Set<BookingItem>();
        public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
		public DbSet<SeedHistory> SeedHistories => Set<SeedHistory>();
        public DbSet<ResetPasswordToken> ResetPasswordTokens => Set<ResetPasswordToken>();

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

            // RowVersion concurrency
            foreach (var et in modelBuilder.Model.GetEntityTypes())
            {
                var prop = et.FindProperty(nameof(BaseEntity.RowVersion));
                if (prop != null)
                {
                    prop.IsConcurrencyToken = true;
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

			// Relationships
			modelBuilder.Entity<UserBranchMembership>()
                .HasOne(m => m.User)
                .WithMany(u => u.BranchMemberships)
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
                .WithMany(u => u.RenterBookings)
                .HasForeignKey(b => b.RenterId);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Staff)
                .WithMany(u => u.StaffBookings)
                .HasForeignKey(b => b.StaffId);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Branch)
                .WithMany(br => br.Bookings)
                .HasForeignKey(b => b.BranchId);

            modelBuilder.Entity<ContractInstance>()
                .HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId);

            modelBuilder.Entity<ContractInstance>()
                .HasOne(c => c.Template)
                .WithMany()
                .HasForeignKey(c => c.TemplateId);

            modelBuilder.Entity<ContractSigner>()
                .HasOne(s => s.Contract)
                .WithMany(c => c.Signers)
                .HasForeignKey(s => s.ContractId);

            modelBuilder.Entity<ContractEvent>()
                .HasOne(e => e.Contract)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.ContractId);

            modelBuilder.Entity<DeliveryTask>()
                .HasOne(t => t.Booking)
                .WithMany()
                .HasForeignKey(t => t.BookingId);

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.OwnerUser)
                .WithMany()
                .HasForeignKey(w => w.OwnerUserId);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Wallet)
                .WithMany()
                .HasForeignKey(t => t.WalletId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany()
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

            modelBuilder.Entity<DeviceCategoryLink>()
                .HasOne(dc => dc.Camera)
                .WithMany(c => c.Categories)
                .HasForeignKey(dc => dc.CameraId);

            modelBuilder.Entity<DeviceCategoryLink>()
                .HasOne(dc => dc.Accessory)
                .WithMany(a => a.Categories)
                .HasForeignKey(dc => dc.AccessoryId);

            modelBuilder.Entity<DeviceCategoryLink>()
                .HasOne(dc => dc.Category)
                .WithMany()
                .HasForeignKey(dc => dc.CategoryId);

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

            modelBuilder.Entity<VerificationRequest>()
                .HasOne(v => v.Staff)
                .WithMany()
                .HasForeignKey(v => v.StaffId);

            modelBuilder.Entity<ResetPasswordToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<VerificationRequest>()
                .HasOne(v => v.Branch)
                .WithMany()
                .HasForeignKey(v => v.BranchId);

            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Booking)
                .WithMany(b => b.Inspections)
                .HasForeignKey(i => i.BookingId);

            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.VerifyRequest)
                .WithMany(v => v.Inspections)
                .HasForeignKey(i => i.VerifyRequestId);

            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Manager)
                .WithMany()
                .HasForeignKey(i => i.ManagerId);

            modelBuilder.Entity<Inspection>()
                .HasOne(i => i.Branch)
                .WithMany()
                .HasForeignKey(i => i.BranchId);
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
    }
}

