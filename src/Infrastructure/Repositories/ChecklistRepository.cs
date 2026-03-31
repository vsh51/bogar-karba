using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChecklistRepository(ApplicationDbContext context) : IChecklistRepository
{
    public IEnumerable<Checklist> GetAll() => context.Checklists;

    public async Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId)
    {
        return await context.Checklists
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task AddAsync(Checklist checklist)
    {
        await context.Checklists.AddAsync(checklist);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var checklist = await context.Checklists.FindAsync(id);
        if (checklist != null)
        {
            context.Checklists.Remove(checklist);
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await context.Checklists.CountAsync();
    }

    public async Task<Checklist?> GetByIdWithSectionsAsync(Guid id)
    {
        return await context.Checklists
            .Include(c => c.Sections.OrderBy(s => s.Position))
            .ThenInclude(s => s.Tasks.OrderBy(t => t.Position))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task UpdateAsync()
    {
        await context.SaveChangesAsync();
    }
}
