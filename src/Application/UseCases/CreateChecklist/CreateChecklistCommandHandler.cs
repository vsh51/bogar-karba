using Application.Common;
using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UseCases.CreateChecklist;

public sealed class CreateChecklistCommandHandler(
    IChecklistRepository repository,
    IOptions<ChecklistOptions> options,
    ILogger<CreateChecklistCommandHandler> logger)
{
    public async Task<Result<Guid>> HandleAsync(CreateChecklistCommand request, string userId)
    {
        logger.LogInformation(
            "Checklist creation requested by user {UserId}: title '{Title}', sections {SectionCount}",
            userId,
            request.Title,
            request.Sections.Count);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            logger.LogWarning("Checklist creation failed for user {UserId}: title is required", userId);
            return "Title is required.";
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deadlineError = DeadlineValidator.Validate(
            request.Deadline, today, options.Value.MaxDeadlineYears);
        if (deadlineError is not null)
        {
            logger.LogWarning(
                "Checklist creation failed for user {UserId}: {Error}", userId, deadlineError);
            return deadlineError;
        }

        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Deadline = request.Deadline,
            Status = ChecklistStatus.Published, // New checklists are published immediately; clones start as Draft.
            Sections = request.Sections.Select(s => new Section
            {
                Id = Guid.NewGuid(),
                Name = s.Name,
                Position = s.Position,
                Tasks = s.Tasks.Select(t => new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Content = t.Content,
                    Position = t.Position,
                    Link = string.IsNullOrWhiteSpace(t.Link) ? null : t.Link.Trim()
                }).ToList()
            }).ToList()
        };

        await repository.AddAsync(checklist);
        logger.LogInformation("Successfully created checklist {ChecklistId} for user {UserId}", checklist.Id, userId);
        return checklist.Id;
    }
}
