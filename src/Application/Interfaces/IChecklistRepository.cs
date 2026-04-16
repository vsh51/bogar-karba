using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistRepository
{
    Task<List<Checklist>> GetAllAsync();

    Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId);

    Task AddAsync(Checklist checklist);

    Task DeleteAsync(Guid id);

    Task UpdateStatusAsync(Guid id, ChecklistStatus newStatus);

    Task<int> GetTotalCountAsync();

    Task<Checklist?> GetByIdWithDetailsAsync(Guid id);

    Task AddTaskAsync(TaskItem task);

    Task UpdateAsync();
}
