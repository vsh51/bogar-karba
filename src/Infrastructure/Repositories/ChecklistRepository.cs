using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChecklistRepository(ApplicationDbContext context) : IChecklistRepository
{
    public async Task<List<Checklist>> GetAllAsync() => await context.Checklists.ToListAsync();

    public async Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId)
    {
        return await context.Checklists
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<Checklist>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.Checklists.Where(c => ids.Contains(c.Id)).AsNoTracking().ToListAsync();
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

    public async Task UpdateStatusAsync(Guid id, ChecklistStatus newStatus)
    {
        var checklist = await context.Checklists.FindAsync(id);
        if (checklist != null)
        {
            checklist.Status = newStatus;
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateVisibilityAsync(Guid id, bool isPublic)
    {
        var checklist = await context.Checklists.FindAsync(id);
        if (checklist != null)
        {
            checklist.IsPublic = isPublic;
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await context.Checklists.CountAsync();
    }

    public async Task<int> GetCountByStatusAsync(ChecklistStatus status)
    {
        return await context.Checklists.CountAsync(c => c.Status == status);
    }

    public async Task<Checklist?> GetByIdWithDetailsAsync(Guid id)
    {
        return await context.Checklists
            .Include(c => c.Sections.OrderBy(s => s.Position))
            .ThenInclude(s => s.Tasks.OrderBy(t => t.Position))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddSectionAsync(Section section)
    {
        await context.Sections.AddAsync(section);
    }

    public async Task AddTaskAsync(TaskItem task)
    {
        await context.Tasks.AddAsync(task);
    }

    public async Task UpdateAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task GrantAccessAsync(ChecklistAccess access)
    {
        await context.ChecklistAccesses.AddAsync(access);
        await context.SaveChangesAsync();
    }

    public async Task RevokeAccessAsync(Guid checklistId, string userId)
    {
        var access = await context.ChecklistAccesses
            .FindAsync(checklistId, userId);
        if (access is not null)
        {
            context.ChecklistAccesses.Remove(access);
            await context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllAccessesAsync(Guid checklistId)
    {
        var accesses = await context.ChecklistAccesses
            .Where(a => a.ChecklistId == checklistId)
            .ToListAsync();

        if (accesses.Count != 0)
        {
            context.ChecklistAccesses.RemoveRange(accesses);
            await context.SaveChangesAsync();
        }
    }

    public async Task GrantCoAuthorAsync(ChecklistCoAuthor coAuthor)
    {
        await context.ChecklistCoAuthors.AddAsync(coAuthor);
        await context.SaveChangesAsync();
    }

    public async Task RevokeCoAuthorAsync(Guid checklistId, string userId)
    {
        var coAuthor = await context.ChecklistCoAuthors
            .FindAsync(checklistId, userId);
        if (coAuthor is not null)
        {
            context.ChecklistCoAuthors.Remove(coAuthor);
            await context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllCoAuthorsAsync(Guid checklistId)
    {
        var coAuthors = await context.ChecklistCoAuthors
            .Where(ca => ca.ChecklistId == checklistId)
            .ToListAsync();

        if (coAuthors.Count != 0)
        {
            context.ChecklistCoAuthors.RemoveRange(coAuthors);
            await context.SaveChangesAsync();
        }
    }
}
