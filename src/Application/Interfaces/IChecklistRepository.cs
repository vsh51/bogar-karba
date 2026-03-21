using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistRepository
{
    IEnumerable<Checklist> GetAll();

    Task DeleteAsync(Guid id);
}
