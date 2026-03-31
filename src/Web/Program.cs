using System.Globalization;
using Application.Interfaces;
using Application.UseCases.Auth.LoginAdmin;
using Application.UseCases.Auth.LoginUser;
using Application.UseCases.Auth.Logout;
using Application.UseCases.Auth.RegisterUser;
using Application.UseCases.BanUser;
using Application.UseCases.CloneChecklist;
using Application.UseCases.CreateChecklist;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.ExportChecklist.Markdown;
using Application.UseCases.GetPublishedChecklist;
using Application.UseCases.GetSystemStats;
using Application.UseCases.GetUserChecklists;
using Application.UseCases.SearchChecklists;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Load environment variables from .env file
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

    var seqUrl = context.Configuration["Seq:Url"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        configuration.WriteTo.Seq(seqUrl, formatProvider: CultureInfo.InvariantCulture);
    }
});

var dbHost = builder.Configuration["DB_HOST"];
var dbPort = builder.Configuration["DB_PORT"];
var dbName = builder.Configuration["POSTGRES_DB"];
var dbUser = builder.Configuration["POSTGRES_USER"];
var dbPassword = builder.Configuration["POSTGRES_PASSWORD"];

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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
builder.Services.AddScoped<CloneChecklistCommandHandler>();
builder.Services.AddScoped<BanUserCommandHandler>();
builder.Services.AddScoped<GetPublishedChecklistQueryHandler>();
builder.Services.AddScoped<GetUserChecklistsQueryHandler>();
builder.Services.AddScoped<CreateChecklistCommandHandler>();
builder.Services.AddScoped<GetSystemStatsQueryHandler>();
builder.Services.AddScoped<ExportMarkdownQueryHandler>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
        .Database.MigrateAsync();

    await AdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
    await ChecklistSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
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
