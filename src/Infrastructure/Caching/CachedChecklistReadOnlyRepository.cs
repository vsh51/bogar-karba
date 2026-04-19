using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Caching;

public sealed class CachedChecklistReadOnlyRepository(
    IChecklistReadOnlyRepository inner,
    IMemoryCache cache,
    IOptions<CacheOptions> options,
    ILogger<CachedChecklistReadOnlyRepository> logger) : IChecklistReadOnlyRepository
{
    public async Task<Checklist?> GetPublishedChecklistAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.PublishedChecklist(id);

        if (cache.TryGetValue(key, out Checklist? cached))
        {
            logger.LogInformation("Cache hit for published checklist {ChecklistId}", id);
            return cached;
        }

        logger.LogInformation("Cache miss for published checklist {ChecklistId}", id);

        var checklist = await inner.GetPublishedChecklistAsync(id, cancellationToken);

        if (checklist is not null)
        {
            var ttl = TimeSpan.FromMinutes(options.Value.PublishedChecklistMinutes);
            cache.Set(key, checklist, ttl);
            logger.LogInformation(
                "Cached published checklist {ChecklistId} for {Minutes} minutes",
                id,
                ttl.TotalMinutes);
        }

        return checklist;
    }

    public Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId) =>
        inner.GetByUserIdAsync(userId);

    public Task<Checklist?> GetByIdAsync(Guid id) =>
        inner.GetByIdAsync(id);

    public Task<Checklist?> GetByIdWithSectionsAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        inner.GetByIdWithSectionsAsync(id, cancellationToken);
}
