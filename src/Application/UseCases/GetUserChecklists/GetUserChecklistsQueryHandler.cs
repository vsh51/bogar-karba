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

        var results = items
            .Select(c => c.ToSummaryDto(today))
            .ToList();

        logger.LogInformation("Found {Count} checklists", results.Count);

        return results;
    }
}
