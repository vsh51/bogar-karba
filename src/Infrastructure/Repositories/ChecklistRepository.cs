using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class ChecklistRepository(ApplicationDbContext context) : IChecklistRepository
{
    public IEnumerable<Checklist> GetAll() => context.Checklists;

    public async Task DeleteAsync(Guid id)
    {
        var checklist = await context.Checklists.FindAsync(id);
        if (checklist != null)
        {
            context.Checklists.Remove(checklist);
            await context.SaveChangesAsync();
        }
    }
}
