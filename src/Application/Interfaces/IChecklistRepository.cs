using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IChecklistRepository
{
    Task<IEnumerable<Checklist>> GetAllAsync();

    Task<Checklist?> GetByIdAsync(Guid id);

    Task DeleteAsync(Guid id);

    Task SaveChangesAsync();
}
