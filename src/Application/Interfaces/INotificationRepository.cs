using Domain.Entities;

namespace Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<List<Notification>> GetUnsentAsync(CancellationToken cancellationToken = default);

    Task MarkSentAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string eventKey, CancellationToken cancellationToken = default);
}
