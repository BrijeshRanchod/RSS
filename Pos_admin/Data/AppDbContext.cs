using Microsoft.EntityFrameworkCore;
using Pos.Models;

namespace Pos.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service>  Services  => Set<Service>();
    public DbSet<Sale>     Sales     => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<SalesPerson> SalesPeople => Set<SalesPerson>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Service>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        b.Entity<SaleLine>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");

        // simple seed: your salon’s fixed menu
        b.Entity<Service>().HasData(
            new Service { Id = 1, Name = "Manicure",        Price = 180m },
            new Service { Id = 2, Name = "Pedicure",        Price = 220m },
            new Service { Id = 3, Name = "Gel Overlay",     Price = 250m },
            new Service { Id = 4, Name = "Acrylic Full Set",Price = 420m },
            new Service { Id = 5, Name = "Soak Off",        Price =  90m },
            new Service { Id = 6, Name = "Nail Art (per)",  Price =  30m }
        );
    }
}
