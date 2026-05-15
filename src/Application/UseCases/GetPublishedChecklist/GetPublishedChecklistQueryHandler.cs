using Application.Common;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetPublishedChecklist;

public sealed class GetPublishedChecklistQueryHandler(
    IChecklistReadOnlyRepository repository,
    ILogger<GetPublishedChecklistQueryHandler> logger)
{
    public async Task<Result<GetPublishedChecklistResult>> HandleAsync(
        GetPublishedChecklistQuery query,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching published checklist {ChecklistId}", query.Id);

        var checklist = await repository.GetByIdWithSectionsAsync(query.Id, cancellationToken);

        if (checklist is null)
        {
            logger.LogInformation("Checklist with id {ChecklistId} was not found", query.Id);
            return ResultErrors.ChecklistNotFound;
        }

        var isOwner = query.OwnerId is not null && checklist.UserId == query.OwnerId;

        if (isOwner)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var ownerResult = checklist.ToPublishedChecklistResult(today, isOwner: true);
            logger.LogInformation("Checklist {ChecklistId} retrieved successfully by owner", query.Id);
            return ownerResult;
        }

        if (checklist.Status != ChecklistStatus.Published)
        {
            logger.LogInformation("Checklist {ChecklistId} is not published and user is not the owner", query.Id);
            return ResultErrors.ChecklistNotFound;
        }

        if (!checklist.IsPublic)
        {
            var hasAccess = query.OwnerId is not null
                && await repository.HasAccessAsync(query.Id, query.OwnerId, cancellationToken);

            if (!hasAccess)
            {
                logger.LogInformation("Checklist {ChecklistId} is private and user does not have access", query.Id);
                return ResultErrors.ChecklistIsPrivate;
            }
        }

        var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = checklist.ToPublishedChecklistResult(todayDate);

        logger.LogInformation("Checklist {ChecklistId} retrieved successfully", query.Id);

        return result;
    }
}
