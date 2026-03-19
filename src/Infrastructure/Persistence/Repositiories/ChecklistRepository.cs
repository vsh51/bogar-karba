using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ChecklistRepository : IChecklistRepository
{
    private readonly ApplicationDbContext _context;

    public ChecklistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Checklist>> GetAllAsync()
    {
        return await _context.Checklists.ToListAsync();
    }

    public async Task<Checklist?> GetByIdAsync(Guid id)
    {
        return await _context.Checklists.FindAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var checklist = await _context.Checklists.FindAsync(id);
        if (checklist != null)
        {
            _context.Checklists.Remove(checklist);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
