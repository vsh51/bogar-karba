using Application.Common;
using Application.DTOs.Checklist;
using Application.Interfaces;
using Application.Mappings;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetUserChecklists;

public sealed class GetUserChecklistsQueryHandler(
    IChecklistReadOnlyRepository repository,
    ILogger<GetUserChecklistsQueryHandler> logger)
{
    public async Task<Result<List<ChecklistSummaryDto>>> HandleAsync(GetUserChecklistsQuery query)
    {
        logger.LogInformation("Fetching checklists for user: {UserId}", query.UserId);

        var items = await repository.GetByUserIdAsync(query.UserId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var results = new List<ChecklistSummaryDto>();
        foreach (var item in items)
        {
            var summary = item.ToSummaryDto(today);
            var isOwner = item.UserId == query.UserId;

            var collaborators = new List<ChecklistAccessDto>();
            if (isOwner)
            {
                collaborators = await repository.GetCollaboratorsAsync(item.Id);
            }

            results.Add(new ChecklistSummaryDto
            {
                Id = summary.Id,
                Title = summary.Title,
                Description = summary.Description,
                UserId = summary.UserId,
                UserName = summary.UserName,
                Status = summary.Status,
                IsPublic = summary.IsPublic,
                Deadline = summary.Deadline,
                IsOutdated = summary.IsOutdated,
                DeadlineRemaining = summary.DeadlineRemaining,
                IsOwner = isOwner,
                Collaborators = collaborators
            });
        }

        logger.LogInformation("Found {Count} checklists", results.Count);

        return results;
    }
}
