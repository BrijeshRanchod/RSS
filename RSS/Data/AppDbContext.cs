// RSSPOS.Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using RSSPOS.Models;

namespace RSSPOS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // OPTIONAL safety net for design-time / accidental direct use:
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Throw a helpful error instead of failing later with "ConnectionString not initialized"
            throw new InvalidOperationException(
                "AppDbContext was created without options. " +
                "Make sure you register it with UseSqlServer(...) in App.xaml.cs and resolve it via DI."
            );
        }
    }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<SalesPerson> SalesPeople => Set<SalesPerson>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
}
