using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pos.Models;

namespace Pos.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Service> Services => Set<Service>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleLine> SaleLines => Set<SaleLine>();
        public DbSet<SalesPerson> SalesPeople => Set<SalesPerson>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<SalesPerson>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique();
                e.HasOne(x => x.IdentityUser)
                 .WithOne()
                 .HasForeignKey<SalesPerson>(x => x.IdentityUserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            b.Entity<Service>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            b.Entity<SaleLine>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
        }
    }
}
