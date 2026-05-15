using Application.Common;
using Application.DTOs.Checklist;
using Application.Interfaces;
using Application.Mappings;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetSharedChecklists;

public sealed class GetSharedChecklistsQueryHandler(
    IChecklistReadOnlyRepository repository,
    ILogger<GetSharedChecklistsQueryHandler> logger)
{
    public async Task<Result<List<ChecklistSummaryDto>>> HandleAsync(GetSharedChecklistsQuery query)
    {
        logger.LogInformation("Fetching shared checklists for user: {UserId}", query.UserId);

        var sharedItems = await repository.GetSharedWithUserAsync(query.UserId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var results = sharedItems
            .Select(s => s.Checklist.ToSummaryDto(today, s.OwnerName))
            .ToList();

        logger.LogInformation("Found {Count} shared checklists", results.Count);

        return results;
    }
}
