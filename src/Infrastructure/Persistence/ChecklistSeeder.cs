using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class ChecklistSeeder
{
    private const string DemoChecklistOwnerUserName = "demochecklist";

    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ChecklistSeeder));
        if (await context.Checklists.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already contains checklists, skipping seeding");
            return;
        }

        var ownerId = await EnsureDemoChecklistOwnerAsync(userManager, configuration, logger);
        if (string.IsNullOrEmpty(ownerId))
        {
            logger.LogWarning(
                "Skipping demo checklist seed: could not resolve user {UserName}.",
                DemoChecklistOwnerUserName);
            return;
        }

        var checklistA = BuildDemoChecklist(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Large demo checklist for scrolling tests (A)",
            ownerId);

        var checklistB = BuildDemoChecklist(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Second demo checklist (B)",
            ownerId);

        context.Checklists.AddRange(checklistA, checklistB);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded demo checklists {ChecklistAId} and {ChecklistBId}",
            checklistA.Id,
            checklistB.Id);
    }

    private static async Task<string?> EnsureDemoChecklistOwnerAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var existing = await userManager.FindByNameAsync(DemoChecklistOwnerUserName);
        if (existing is not null)
        {
            return existing.Id;
        }

        var password = configuration["Seed:DemoChecklistOwnerPassword"];
        if (string.IsNullOrEmpty(password))
        {
            logger.LogError(
                "Cannot create {UserName}: configure Seed:DemoChecklistOwnerPassword.",
                DemoChecklistOwnerUserName);
            return null;
        }

        var email = configuration["Seed:DemoChecklistOwnerEmail"];
        if (string.IsNullOrEmpty(password))
        {
            logger.LogError(
                "Cannot create {UserName}: configure Seed:DemoChecklistOwnerEmail.",
                DemoChecklistOwnerUserName);
            return null;
        }

        var user = new ApplicationUser
        {
            UserName = DemoChecklistOwnerUserName,
            Email = email,
            EmailConfirmed = true,
            AccountStatus = UserStatus.Active,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError(
                "Failed to create seed user {UserName}: {Errors}",
                DemoChecklistOwnerUserName,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return null;
        }

        return user.Id;
    }

    private static Checklist BuildDemoChecklist(Guid id, string title, string ownerId)
    {
        return new Checklist
        {
            Id = id,
            Title = title,
            Description = "This seeded checklist intentionally contains many sections and items so you can validate layout, typography, spacing, and long-page scrolling behavior in the UI.",
            Status = ChecklistStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UserId = ownerId,
            Sections = BuildDemoSections()
        };
    }

    private static List<Section> BuildDemoSections()
    {
        var sections = new List<Section>();

        for (var sectionNumber = 1; sectionNumber <= 8; sectionNumber++)
        {
            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = $"Section {sectionNumber}",
                Position = sectionNumber
            };

            for (var itemNumber = 1; itemNumber <= 9; itemNumber++)
            {
                section.Tasks.Add(new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Position = itemNumber,
                    Content = $"Section {sectionNumber} item {itemNumber}: check readability with longer text in a realistic checklist row."
                });
            }

            sections.Add(section);
        }

        return sections;
    }
}
