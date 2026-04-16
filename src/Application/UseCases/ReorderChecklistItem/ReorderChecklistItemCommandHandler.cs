using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ReorderChecklistItem;

public sealed class ReorderChecklistItemCommandHandler(
    IChecklistRepository repository,
    ILogger<ReorderChecklistItemCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(ReorderChecklistItemCommand command)
    {
        logger.LogInformation(
            "Reordering item {TaskId} in checklist {ChecklistId} to section {TargetSectionId} position {NewPosition} by user {OwnerId}",
            command.TaskId,
            command.ChecklistId,
            command.TargetSectionId,
            command.NewPosition,
            command.OwnerId);

        if (command.NewPosition < 0)
        {
            return "Position must be non-negative.";
        }

        var checklist = await repository.GetByIdWithDetailsAsync(command.ChecklistId);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {ChecklistId} not found", command.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to modify checklist {ChecklistId} owned by {ActualOwner}",
                command.OwnerId,
                command.ChecklistId,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        var sourceSection = checklist.Sections.FirstOrDefault(s => s.Tasks.Any(t => t.Id == command.TaskId));
        var targetSection = checklist.Sections.FirstOrDefault(s => s.Id == command.TargetSectionId);

        if (sourceSection is null)
        {
            return "Item not found.";
        }

        if (targetSection is null)
        {
            return "Target section not found.";
        }

        var task = sourceSection.Tasks.First(t => t.Id == command.TaskId);
        sourceSection.Tasks.Remove(task);
        task.SectionId = targetSection.Id;

        var insertAt = Math.Clamp(command.NewPosition, 0, targetSection.Tasks.Count);
        targetSection.Tasks.Insert(insertAt, task);

        Resequence(sourceSection.Tasks);
        if (sourceSection.Id != targetSection.Id)
        {
            Resequence(targetSection.Tasks);
        }

        await repository.UpdateAsync();
        logger.LogInformation(
            "Reordered item {TaskId} into section {SectionId} at position {Position}",
            task.Id,
            targetSection.Id,
            insertAt);
        return true;
    }

    private static void Resequence(List<Domain.Entities.TaskItem> tasks)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            tasks[i].Position = i;
        }
    }
}
