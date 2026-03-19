using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistReadOnlyRepository
{
    Task<Checklist?> GetPublishedChecklistAsync(Guid id, CancellationToken cancellationToken = default);
}
