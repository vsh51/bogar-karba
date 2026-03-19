using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

public class ChecklistRepository(ApplicationDbContext context) : IChecklistRepository
{
    public IQueryable<Checklist> GetAll() => context.Checklists;
}
