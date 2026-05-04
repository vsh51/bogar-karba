using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Notifications.AddAsync(notification, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetUnsentAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .Where(n => !n.IsSent)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkSentAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Notifications
            .Where(n => ids.Contains(n.Id))
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.IsSent, true),
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string eventKey,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.EventKey == eventKey, cancellationToken);
    }
}
