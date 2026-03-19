using System.Globalization;
using Application.Interfaces;
using Application.UseCases;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var seqUrl = builder.Configuration.GetValue<string>("Seq:Url")
    ?? throw new InvalidOperationException("Seq URL is not configured.");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.Seq(seqUrl, formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IChecklistRepository, ChecklistRepository>();
builder.Services.AddScoped<SearchChecklistsService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment() && !await db.Checklists.AnyAsync())
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "admin",
            PasswordHash = "password",
            AccountStatus = UserStatus.Active
        };

        db.Users.Add(user);

        db.Checklists.AddRange(
            new Checklist
            {
                Title = "Example checklist",
                Description = "This is sample data that you can search for.",
                UserId = user.Id,
                Author = user
            },
            new Checklist
            {
                Title = "Another checklist",
                Description = "Try searching for 'another' or 'example'.",
                UserId = user.Id,
                Author = user
            });

        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
