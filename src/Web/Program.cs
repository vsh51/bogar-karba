using System.Globalization;
using Application.Interfaces;
using Application.UseCases.AdminAuth;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
#pragma warning disable S1075 // URIs should not be hardcoded
    .WriteTo.Seq(builder.Configuration["Serilog:SeqServerUrl"] ?? "http://seq:5341", formatProvider: CultureInfo.InvariantCulture)
#pragma warning restore S1075
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Login";
    options.AccessDeniedPath = "/Admin/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
});

builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminSignInService, AdminSignInService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await SeedAdminAsync(scope.ServiceProvider, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();

static async Task SeedAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    const string adminRoleName = "Admin";
    const string adminUserName = "admin";
    string initialAdminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";

    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));
    }

    var adminUser = await userManager.FindByNameAsync(adminUserName);
    if (adminUser is null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminUserName,
            AccountStatus = UserStatus.Active,
        };

        var result = await userManager.CreateAsync(adminUser, initialAdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminRoleName);
        }
    }
}
