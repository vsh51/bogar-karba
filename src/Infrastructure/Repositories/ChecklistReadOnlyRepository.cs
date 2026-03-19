using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public sealed class ChecklistReadOnlyRepository : IChecklistReadOnlyRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChecklistReadOnlyRepository> _logger;

    public ChecklistReadOnlyRepository(
        ApplicationDbContext dbContext,
        ILogger<ChecklistReadOnlyRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Checklist?> GetPublishedChecklistAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving published checklist {ChecklistId} from database", id);

        return await _dbContext.Checklists
            .AsNoTracking()
            .Include(c => c.Sections.OrderBy(s => s.Position))
            .ThenInclude(s => s.Tasks.OrderBy(t => t.Position))
            .Where(c => c.Id == id && c.Status == ChecklistStatus.Published)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
