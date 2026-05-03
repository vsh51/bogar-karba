using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetChecklistProgress;

public sealed class GetChecklistProgressQueryHandler(
    IChecklistReadOnlyRepository checklistReadOnlyRepository,
    IChecklistProgressRepository checklistProgressRepository,
    ILogger<GetChecklistProgressQueryHandler> logger)
{
    public async Task<Result<IReadOnlyList<Guid>>> HandleAsync(
        GetChecklistProgressQuery query,
        CancellationToken cancellationToken = default)
    {
        var checklist = await checklistReadOnlyRepository.GetByIdWithSectionsAsync(query.ChecklistId, cancellationToken);
        if (checklist is null)
        {
            logger.LogInformation("Progress read failed: checklist {ChecklistId} not found", query.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != query.UserId)
        {
            logger.LogInformation(
                "Progress read denied for checklist {ChecklistId}: user {UserId} is not owner",
                query.ChecklistId,
                query.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        var completedTaskIds = await checklistProgressRepository.GetCompletedTaskIdsAsync(
            query.ChecklistId,
            query.UserId,
            cancellationToken);

        var validTaskIds = checklist.Sections
            .SelectMany(s => s.Tasks)
            .Select(t => t.Id)
            .ToHashSet();

        var filtered = completedTaskIds
            .Where(id => validTaskIds.Contains(id))
            .Distinct()
            .ToList();

        return filtered;
    }
}
