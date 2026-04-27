using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SaveChecklistProgress;

public sealed class SaveChecklistProgressCommandHandler(
    IChecklistReadOnlyRepository checklistReadOnlyRepository,
    IChecklistProgressRepository checklistProgressRepository,
    ILogger<SaveChecklistProgressCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(
        SaveChecklistProgressCommand command,
        CancellationToken cancellationToken = default)
    {
        var checklist = await checklistReadOnlyRepository.GetByIdWithSectionsAsync(command.ChecklistId, cancellationToken);
        if (checklist is null)
        {
            logger.LogInformation("Progress save failed: checklist {ChecklistId} not found", command.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != command.UserId)
        {
            logger.LogInformation(
                "Progress save denied for checklist {ChecklistId}: user {UserId} is not owner",
                command.ChecklistId,
                command.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        var validTaskIds = checklist.Sections
            .SelectMany(s => s.Tasks)
            .Select(t => t.Id)
            .ToHashSet();

        var filtered = (command.CompletedTaskIds ?? Array.Empty<Guid>())
            .Where(id => validTaskIds.Contains(id))
            .Distinct()
            .ToList();

        await checklistProgressRepository.SaveCompletedTaskIdsAsync(
            command.ChecklistId,
            command.UserId,
            filtered,
            cancellationToken);

        logger.LogInformation(
            "Progress saved for checklist {ChecklistId} by owner {UserId}. Completed items: {Count}",
            command.ChecklistId,
            command.UserId,
            filtered.Count);

        return true;
    }
}
