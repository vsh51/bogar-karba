using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistRepository
{
    IEnumerable<Checklist> GetAll();

    IEnumerable<Checklist> GetByUserId(string userId);

    Task<Checklist?> GetByIdAsync(Guid id);

    Task DeleteAsync(Guid id);
}
