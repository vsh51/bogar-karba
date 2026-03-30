using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistRepository
{
    IEnumerable<Checklist> GetAll();

    Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId);

    Task AddAsync(Checklist checklist);

    Task DeleteAsync(Guid id);
}
