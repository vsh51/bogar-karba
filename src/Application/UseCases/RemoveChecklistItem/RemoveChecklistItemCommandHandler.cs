using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.RemoveChecklistItem;

public sealed class RemoveChecklistItemCommandHandler(
    IChecklistRepository repository,
    ILogger<RemoveChecklistItemCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(RemoveChecklistItemCommand command)
    {
        logger.LogInformation(
            "Removing item {TaskId} from checklist {ChecklistId} by user {OwnerId}",
            command.TaskId,
            command.ChecklistId,
            command.OwnerId);

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

        var section = checklist.Sections.FirstOrDefault(s => s.Tasks.Any(t => t.Id == command.TaskId));

        if (section is null)
        {
            return "Item not found.";
        }

        var task = section.Tasks.First(t => t.Id == command.TaskId);
        section.Tasks.Remove(task);

        for (var i = 0; i < section.Tasks.Count; i++)
        {
            section.Tasks[i].Position = i;
        }

        await repository.UpdateAsync();
        logger.LogInformation("Removed item {TaskId} from section {SectionId}", command.TaskId, section.Id);
        return true;
    }
}
