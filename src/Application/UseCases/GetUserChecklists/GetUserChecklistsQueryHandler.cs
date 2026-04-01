using Application.Common;
using Application.DTOs.Checklist;
using Application.Interfaces;
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

        var results = items
            .Select(c => new ChecklistSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                UserId = c.UserId
            })
            .ToList();

        logger.LogInformation("Found {Count} checklists", results.Count);

        return results;
    }
}
