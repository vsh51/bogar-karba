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

    public async Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId)
    {
        return await dbContext.Checklists
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task<Checklist?> GetByIdAsync(Guid id)
    {
        return await dbContext.Checklists
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Checklist?> GetByIdWithSectionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Checklists
            .AsNoTracking()
            .Include(c => c.Sections.OrderBy(s => s.Position))
            .ThenInclude(s => s.Tasks.OrderBy(t => t.Position))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> HasAccessAsync(Guid checklistId, string userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ChecklistAccesses
            .AsNoTracking()
            .AnyAsync(a => a.ChecklistId == checklistId && a.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<SharedChecklist>> GetSharedWithUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving private checklists shared with user {UserId}", userId);

        return await dbContext.ChecklistAccesses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.Checklist)
            .Where(c => !c.IsPublic && c.Status == ChecklistStatus.Published)
            .Join(
                dbContext.Users,
                checklist => checklist.UserId,
                user => user.Id,
                (checklist, user) => new SharedChecklist(checklist, $"{user.Name} {user.Surname}"))
            .ToListAsync(cancellationToken);
    }
}
