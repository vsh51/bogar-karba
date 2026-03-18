using Application.DTOs.Checklist;
using Application.UseCases.GetPublishedChecklist;
using Domain.Entities;

namespace Application.Mappings;

public static class ChecklistMappings
{
    public static GetPublishedChecklistResult ToPublishedChecklistResult(
        this Checklist checklist)
    {
        return new GetPublishedChecklistResult
        {
            Id = checklist.Id,
            Title = checklist.Title,
            Description = checklist.Description,
            Sections = checklist.Sections
                .OrderBy(s => s.Position)
                .Select(section => new ChecklistSectionDto
                {
                    Id = section.Id,
                    Name = section.Name,
                    Position = section.Position,
                    Items = section.Tasks
                        .OrderBy(t => t.Position)
                        .Select(task => new ChecklistItemDto
                        {
                            Id = task.Id,
                            Content = task.Content,
                            Position = task.Position
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
