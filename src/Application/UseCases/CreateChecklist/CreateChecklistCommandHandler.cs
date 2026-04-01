using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.CreateChecklist;

public class CreateChecklistCommandHandler(
    IChecklistRepository repository,
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

        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
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
                    Position = t.Position
                }).ToList()
            }).ToList()
        };

        await repository.AddAsync(checklist);
        logger.LogInformation("Successfully created checklist {ChecklistId} for user {UserId}", checklist.Id, userId);
        return checklist.Id;
    }
}
