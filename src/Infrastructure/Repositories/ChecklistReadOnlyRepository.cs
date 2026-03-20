using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public sealed class ChecklistReadOnlyRepository(
    ApplicationDbContext dbContext,
    ILogger<ChecklistReadOnlyRepository> logger) : IChecklistReadOnlyRepository
{
    public async Task<Checklist?> GetPublishedChecklistAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving published checklist {ChecklistId} from database", id);

        return await dbContext.Checklists
            .AsNoTracking()
            .Include(c => c.Sections.OrderBy(s => s.Position))
            .ThenInclude(s => s.Tasks.OrderBy(t => t.Position))
            .Where(c => c.Id == id && c.Status == ChecklistStatus.Published)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
