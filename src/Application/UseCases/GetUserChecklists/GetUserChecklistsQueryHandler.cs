using Application.DTOs.Checklist;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetUserChecklists;

public partial class GetUserChecklistsQueryHandler(
    IChecklistReadOnlyRepository repository,
    ILogger<GetUserChecklistsQueryHandler> logger)
{
    public async Task<GetUserChecklistsResult> HandleAsync(GetUserChecklistsQuery query)
    {
        LogRequest(logger, query.UserId);

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

        LogResult(logger, results.Count);

        return new GetUserChecklistsResult(results);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching checklists for user: {UserId}")]
    static partial void LogRequest(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} checklists")]
    static partial void LogResult(ILogger logger, int count);
}
