using Domain.Entities;

namespace Application.Interfaces;

public interface IChecklistRepository
{
    IQueryable<Checklist> GetAll();

    Task DeleteAsync(Guid id);
}
