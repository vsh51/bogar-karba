using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.CloneChecklist;

public sealed class CloneChecklistCommandHandler(
    IChecklistRepository repository,
    IChecklistReadOnlyRepository readRepository,
    ILogger<CloneChecklistCommandHandler> logger)
{
    public async Task<Result<Guid>> HandleAsync(CloneChecklistCommand command)
    {
        logger.LogInformation(
            "Initiated cloning of checklist {ChecklistId} for user {UserId}",
            command.SourceChecklistId,
            command.OwnerId);

        try
        {
            var sourceChecklist = await readRepository.GetByIdWithSectionsAsync(command.SourceChecklistId);

            if (sourceChecklist is null)
            {
                logger.LogWarning("Checklist {ChecklistId} not found for cloning", command.SourceChecklistId);
                return ResultErrors.ChecklistNotFound;
            }

            if (sourceChecklist.UserId != command.OwnerId)
            {
                logger.LogWarning(
                    "User {OwnerId} attempted to clone checklist {ChecklistId} owned by {ActualOwner}",
                    command.OwnerId,
                    sourceChecklist.Id,
                    sourceChecklist.UserId);
                return ResultErrors.NotChecklistOwner;
            }

            // Clones start as Draft so the author can review before publishing.
            var clonedChecklist = new Checklist
            {
                Id = Guid.NewGuid(),
                Title = BuildCopyTitle(sourceChecklist.Title),
                Description = sourceChecklist.Description,
                Status = ChecklistStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UserId = sourceChecklist.UserId,
                Sections = sourceChecklist.Sections
                    .OrderBy(section => section.Position)
                    .Select(section => new Section
                    {
                        Id = Guid.NewGuid(),
                        Name = section.Name,
                        Position = section.Position,
                        Tasks = section.Tasks
                            .OrderBy(task => task.Position)
                            .Select(task => new TaskItem
                            {
                                Id = Guid.NewGuid(),
                                Content = task.Content,
                                Position = task.Position
                            })
                            .ToList()
                    })
                    .ToList()
            };

            await repository.AddAsync(clonedChecklist);

            logger.LogInformation(
                "Checklist {SourceChecklistId} cloned to {ClonedChecklistId}",
                command.SourceChecklistId,
                clonedChecklist.Id);

            return clonedChecklist.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cloning checklist {ChecklistId}", command.SourceChecklistId);
            return "An error occurred while cloning the checklist.";
        }
    }

    private static string BuildCopyTitle(string title)
    {
        return string.IsNullOrWhiteSpace(title)
            ? "(Copy)"
            : $"{title} (Copy)";
    }
}
