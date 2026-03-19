using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

public class ChecklistRepository(ApplicationDbContext context) : IChecklistRepository
{
    public IQueryable<Checklist> GetAll() => context.Checklists;

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
