using Application.Common;
using Application.DTOs.Checklist;
using Application.UseCases.GetPublishedChecklist;
using Domain.Entities;

namespace Application.Mappings;

public static class ChecklistMappings
{
    public static GetPublishedChecklistResult ToPublishedChecklistResult(
        this Checklist checklist,
        DateOnly today)
    {
        var info = DeadlineFormatter.Describe(checklist.Deadline, today);

        return new GetPublishedChecklistResult
        {
            Id = checklist.Id,
            Title = checklist.Title,
            Description = checklist.Description,
            Deadline = checklist.Deadline,
            IsOutdated = info?.IsOutdated ?? false,
            DeadlineRemaining = info?.RemainingText,
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

    public static ChecklistSummaryDto ToSummaryDto(
        this Checklist checklist,
        DateOnly today,
        string? userName = null)
    {
        var info = DeadlineFormatter.Describe(checklist.Deadline, today);

        return new ChecklistSummaryDto
        {
            Id = checklist.Id,
            Title = checklist.Title,
            Description = checklist.Description,
            UserId = checklist.UserId,
            UserName = userName ?? string.Empty,
            Status = checklist.Status,
            Deadline = checklist.Deadline,
            IsOutdated = info?.IsOutdated ?? false,
            DeadlineRemaining = info?.RemainingText
        };
    }
}
