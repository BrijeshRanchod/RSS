// App.xaml.cs
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RSSPOS.Data;
using RSSPOS.ViewModels;

namespace RSSPOS;

public partial class App : Application
{
    public static IHost HostRoot { get; private set; } = null!;

    public App()
    {
        HostRoot = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var cs = ctx.Configuration.GetConnectionString("Default");
                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException("Missing ConnectionStrings:Default in appsettings.json");

                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlServer(cs, sql => sql.EnableRetryOnFailure()));

                services.AddTransient<PosViewModel>();
                services.AddTransient<MainWindow>(sp =>
                    new MainWindow(sp.GetRequiredService<PosViewModel>()));
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await HostRoot.StartAsync();

        // Sanity check: try opening a connection (will catch bad firewall/creds/CS)
        using var scope = HostRoot.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            if (!await db.Database.CanConnectAsync())
                MessageBox.Show("Cannot connect to Azure SQL. Check firewall/connection string.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("DB connection error: " + ex.Message);
        }

        var win = HostRoot.Services.GetRequiredService<MainWindow>();
        win.Show();
        base.OnStartup(e);
    }
}
