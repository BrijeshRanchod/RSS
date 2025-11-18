using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pos.Data;
using Pos.Models; // for SalesRole

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // or UseSqlite

// ✅ Register Identity ONCE (with options)
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(o =>
    {
        o.User.RequireUniqueEmail = true;

        // Lockout policy
        o.Lockout.AllowedForNewUsers = true;
        o.Lockout.MaxFailedAccessAttempts = 8;          
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2); 
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Migrate + seed/link Identity users & roles
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedAndSyncAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Define this in Program.cs (below) or move to a separate static class
static async Task SeedAndSyncAsync(IServiceProvider sp)
{
    var db = sp.GetRequiredService<AppDbContext>();
    var users = sp.GetRequiredService<UserManager<IdentityUser>>();
    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var r in new[] { "Admin", "Manager", "Sales" })
        if (!await roles.RoleExistsAsync(r))
            await roles.CreateAsync(new IdentityRole(r));

    var people = await db.SalesPeople.ToListAsync();
    foreach (var sperson in people)
    {
        if (string.IsNullOrWhiteSpace(sperson.Email)) continue;

        var user = sperson.IdentityUserId is not null
            ? await users.FindByIdAsync(sperson.IdentityUserId!)
            : await users.FindByEmailAsync(sperson.Email);

        if (user is null)
        {
            user = new IdentityUser { UserName = sperson.Email, Email = sperson.Email, EmailConfirmed = true };
            var created = await users.CreateAsync(user, "ICNails2025!");
            if (!created.Succeeded) continue;
        }

        if (sperson.IdentityUserId != user.Id)
            sperson.IdentityUserId = user.Id;

        var desiredRole = sperson.Role switch
        {
            SalesRole.Admin => "Admin",
            SalesRole.Manager => "Manager",
            _ => "Sales"
        };

        foreach (var r in new[] { "Admin", "Manager", "Sales" })
            if (r != desiredRole && await users.IsInRoleAsync(user, r))
                await users.RemoveFromRoleAsync(user, r);
        if (!await users.IsInRoleAsync(user, desiredRole))
            await users.AddToRoleAsync(user, desiredRole);
    }

    await db.SaveChangesAsync();
}
