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

    Task<int> GetCountByStatusAsync(ChecklistStatus status);

    Task<Checklist?> GetByIdWithDetailsAsync(Guid id);

    Task UpdateAsync();
}
