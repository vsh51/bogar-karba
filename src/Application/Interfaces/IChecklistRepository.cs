using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistRepository
{
    Task<List<Checklist>> GetAllAsync();

    Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId);

    Task<List<Checklist>> GetByIdsAsync(IEnumerable<Guid> ids);

    Task AddAsync(Checklist checklist);

    Task DeleteAsync(Guid id);

    Task UpdateStatusAsync(Guid id, ChecklistStatus newStatus);

    Task UpdateVisibilityAsync(Guid id, bool isPublic);

    Task<int> GetTotalCountAsync();

    Task<int> GetCountByStatusAsync(ChecklistStatus status);

    Task<Checklist?> GetByIdWithDetailsAsync(Guid id);

    Task AddSectionAsync(Section section);

    Task AddTaskAsync(TaskItem task);

    Task UpdateAsync();

    Task GrantAccessAsync(ChecklistAccess access);

    Task RevokeAccessAsync(Guid checklistId, string userId);

    Task RevokeAllAccessesAsync(Guid checklistId);

    Task GrantCoAuthorAsync(ChecklistCoAuthor coAuthor);

    Task RevokeCoAuthorAsync(Guid checklistId, string userId);

    Task RevokeAllCoAuthorsAsync(Guid checklistId);
}
