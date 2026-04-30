using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistReadOnlyRepository
{
    Task<Checklist?> GetPublishedChecklistAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId);

    Task<Checklist?> GetByIdAsync(Guid id);

    Task<Checklist?> GetByIdWithSectionsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasAccessAsync(Guid checklistId, string userId, CancellationToken cancellationToken = default);

    Task<List<string>> GetAccessUserIdsAsync(Guid checklistId, CancellationToken cancellationToken = default);

    Task<bool> IsCoAuthorAsync(Guid checklistId, string userId, CancellationToken cancellationToken = default);

    Task<List<string>> GetCoAuthorIdsAsync(Guid checklistId, CancellationToken cancellationToken = default);

    Task<bool> HasWritingPermissionAsync(Guid checklistId, string userId, CancellationToken cancellationToken = default);
}
