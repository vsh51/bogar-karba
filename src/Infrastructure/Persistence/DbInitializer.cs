using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await context.Checklists.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already contains checklists. Skipping seeding.");
            return;
        }

        logger.LogInformation("Seeding initial demo data...");

        var demoUserId = Guid.NewGuid();
        var demoChecklistId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var user = new User
        {
            Id = demoUserId,
            Login = "demo",
            PasswordHash = "not-a-real-password",
            AccountStatus = UserStatus.Active
        };

        var checklist = new Checklist
        {
            Id = demoChecklistId,
            Title = "Large demo checklist for scrolling tests",
            Description = "This seeded checklist intentionally contains many sections and items so you can validate layout, typography, spacing, and long-page scrolling behavior in the UI.",
            Status = ChecklistStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UserId = demoUserId,
            Author = user,
            Sections = BuildLargeDemoSections()
        };

        context.Users.Add(user);
        context.Checklists.Add(checklist);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeding completed. Demo checklist id: {ChecklistId}", demoChecklistId);
    }

    private static List<Section> BuildLargeDemoSections()
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
