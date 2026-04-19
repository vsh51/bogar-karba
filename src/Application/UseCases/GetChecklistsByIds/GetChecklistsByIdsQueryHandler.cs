using Application.Common;
using Application.DTOs.Checklist;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetChecklistsByIds;

public sealed class GetChecklistsByIdsQueryHandler(
    IChecklistRepository repository,
    ILogger<GetChecklistsByIdsQueryHandler> logger)
{
    public async Task<Result<List<ChecklistSummaryDto>>> HandleAsync(GetChecklistsByIdsQuery query)
    {
        logger.LogInformation("Fetching checklists by IDs. Requested count: {Count}", query.Ids.Count);

        if (query.Ids.Count == 0)
        {
            return new List<ChecklistSummaryDto>();
        }

        var checklists = await repository.GetByIdsAsync(query.Ids);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = checklists
            .Where(c => c.Status == ChecklistStatus.Published)
            .Select(c => c.ToSummaryDto(today))
            .ToList();

        logger.LogInformation("Successfully retrieved {Count} published checklists from database", result.Count);

        return result;
    }
}
