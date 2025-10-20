using CamRent_Domain.Bookings;
using CamRent_Domain.Branches;
using CamRent_Domain.Common;
using CamRent_Domain.Contracts;
using CamRent_Domain.Delivery;
using CamRent_Domain.Devices;
using CamRent_Domain.Disputes;
using CamRent_Domain.Files;
using CamRent_Domain.Finance;
using CamRent_Domain.Inspections;
using CamRent_Domain.Reviews;
using CamRent_Domain.Users;
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

        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Inspection> Inspections => Set<Inspection>();
        public DbSet<InspectionItem> InspectionItems => Set<InspectionItem>();
        public DbSet<ContractTemplate> ContractTemplates => Set<ContractTemplate>();
        public DbSet<ContractInstance> Contracts => Set<ContractInstance>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<DeliveryTask> DeliveryTasks => Set<DeliveryTask>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Dispute> Disputes => Set<Dispute>();
        public DbSet<DisputeItem> DisputeItems => Set<DisputeItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // snake_case convention
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.GetColumnBaseName()));
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

            // Relationships
            modelBuilder.Entity<UserBranchMembership>()
                .HasOne(m => m.User)
                .WithMany(u => u.BranchMemberships)
                .HasForeignKey(m => m.UserId);

            modelBuilder.Entity<UserRoleMapping>()
                .HasOne(m => m.User)
                .WithMany(u => u.Roles)
                .HasForeignKey(m => m.UserId);

            modelBuilder.Entity<Device>()
                .HasOne(d => d.Branch)
                .WithMany()
                .HasForeignKey(d => d.BranchId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Device)
                .WithMany()
                .HasForeignKey(b => b.DeviceId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Renter)
                .WithMany()
                .HasForeignKey(b => b.RenterId);

            modelBuilder.Entity<InspectionItem>()
                .HasOne(i => i.Inspection)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.InspectionId);

            modelBuilder.Entity<ContractInstance>()
                .HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId);

            modelBuilder.Entity<ContractInstance>()
                .HasOne(c => c.Template)
                .WithMany()
                .HasForeignKey(c => c.TemplateId);

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

            modelBuilder.Entity<DisputeItem>()
                .HasOne(i => i.Dispute)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.DisputeId);
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

