using Microsoft.EntityFrameworkCore;
using Pos.Data;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// ✅ Load environment variables from .env (for local dev)
DotNetEnv.Env.Load();

// ✅ Ensure .env variables are also added to configuration
builder.Configuration.AddEnvironmentVariables();

// Add MVC
builder.Services.AddControllersWithViews();

// Add EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Substitute any ${VAR} placeholders manually if needed
    connectionString = connectionString
        .Replace("${DB_SERVER}", Environment.GetEnvironmentVariable("DB_SERVER"))
        .Replace("${DB_NAME}", Environment.GetEnvironmentVariable("DB_NAME"))
        .Replace("${DB_USER}", Environment.GetEnvironmentVariable("DB_USER"))
        .Replace("${DB_PASS}", Environment.GetEnvironmentVariable("DB_PASS"));

    options.UseSqlServer(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
