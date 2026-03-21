using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class ChecklistSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ChecklistSeeder));
        if (await context.Checklists.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already contains checklists, skipping seeding");
            return;
        }

        var ownerId = await GetDefaultOwnerIdAsync(context, cancellationToken);
        var checklist = BuildDemoChecklist(ownerId);

        context.Checklists.Add(checklist);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded demo checklist {ChecklistId}", checklist.Id);
    }

    private static async Task<string> GetDefaultOwnerIdAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var firstUser = await context.Users.FirstOrDefaultAsync(cancellationToken);
        return firstUser?.Id ?? string.Empty;
    }

    private static Checklist BuildDemoChecklist(string ownerId)
    {
        return new Checklist
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Title = "Large demo checklist for scrolling tests",
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
