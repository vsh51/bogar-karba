using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Caching;

public sealed class CachedChecklistRepository(
    IChecklistRepository inner,
    IMemoryCache cache,
    ILogger<CachedChecklistRepository> logger) : IChecklistRepository
{
    public Task<List<Checklist>> GetAllAsync() => inner.GetAllAsync();

    public Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId) =>
        inner.GetByUserIdAsync(userId);

    public Task<List<Checklist>> GetByIdsAsync(IEnumerable<Guid> ids) =>
        inner.GetByIdsAsync(ids);

    public Task AddAsync(Checklist checklist) => inner.AddAsync(checklist);

    public async Task DeleteAsync(Guid id)
    {
        await inner.DeleteAsync(id);
        Evict(id);
    }

    public async Task UpdateStatusAsync(Guid id, ChecklistStatus newStatus)
    {
        await inner.UpdateStatusAsync(id, newStatus);
        Evict(id);
    }

    public async Task UpdateVisibilityAsync(Guid id, bool isPublic)
    {
        await inner.UpdateVisibilityAsync(id, isPublic);
        Evict(id);
    }

    public Task<int> GetTotalCountAsync() => inner.GetTotalCountAsync();

    public Task<int> GetCountByStatusAsync(ChecklistStatus status) =>
        inner.GetCountByStatusAsync(status);

    public Task<Checklist?> GetByIdWithDetailsAsync(Guid id) =>
        inner.GetByIdWithDetailsAsync(id);

    public Task AddSectionAsync(Section section) => inner.AddSectionAsync(section);

    public Task AddTaskAsync(TaskItem task) => inner.AddTaskAsync(task);

    public Task UpdateAsync() => inner.UpdateAsync();

    private void Evict(Guid id)
    {
        var key = CacheKeys.PublishedChecklist(id);
        cache.Remove(key);
        logger.LogInformation("Evicted cache entry {CacheKey}", key);
    }
}
