using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetChecklistForEdit;

public sealed class GetChecklistForEditQueryHandler(
    IChecklistReadOnlyRepository repository,
    ILogger<GetChecklistForEditQueryHandler> logger)
{
    public async Task<Result<GetChecklistForEditResult>> HandleAsync(GetChecklistForEditQuery query)
    {
        logger.LogInformation(
            "Fetching checklist {ChecklistId} for editing by user {UserId}",
            query.ChecklistId,
            query.OwnerId);

        var checklist = await repository.GetByIdWithSectionsAsync(query.ChecklistId);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {ChecklistId} not found for editing", query.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != query.OwnerId)
        {
            logger.LogWarning(
                "User {UserId} attempted to edit checklist {ChecklistId} owned by {ActualOwner}",
                query.OwnerId,
                checklist.Id,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        var result = new GetChecklistForEditResult(
            checklist.Id,
            checklist.Title,
            checklist.Description,
            checklist.Sections
                .OrderBy(s => s.Position)
                .Select(s => new EditSectionResult(
                    s.Id,
                    s.Name,
                    s.Tasks
                        .OrderBy(t => t.Position)
                        .Select(t => new EditTaskResult(t.Id, t.Content))
                        .ToList()))
                .ToList());

        logger.LogInformation("Checklist {ChecklistId} loaded for editing", query.ChecklistId);

        return result;
    }
}
