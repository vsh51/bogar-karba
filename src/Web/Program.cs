using System.Globalization;
using Application.Interfaces;
using Application.UseCases.Auth.LoginAdmin;
using Application.UseCases.Auth.LoginUser;
using Application.UseCases.Auth.Logout;
using Application.UseCases.Auth.RegisterUser;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.GetPublishedChecklist;
using Application.UseCases.SearchChecklists;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
});

builder.Services.AddScoped<IChecklistRepository, ChecklistRepository>();
builder.Services.AddScoped<IChecklistReadOnlyRepository, ChecklistReadOnlyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISignInService, SignInService>();

builder.Services.AddScoped<LoginUserCommandHandler>();
builder.Services.AddScoped<LoginAdminCommandHandler>();
builder.Services.AddScoped<LogoutCommandHandler>();
builder.Services.AddScoped<RegisterUserCommandHandler>();
builder.Services.AddScoped<SearchChecklistsQueryHandler>();
builder.Services.AddScoped<DeleteChecklistCommandHandler>();
builder.Services.AddScoped<GetPublishedChecklistQueryHandler>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await SeedAdminAsync(scope.ServiceProvider, app.Configuration);

    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var seedingLogger = loggerFactory.CreateLogger("DbInitializer");
    await DbInitializer.SeedAsync(db, seedingLogger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
