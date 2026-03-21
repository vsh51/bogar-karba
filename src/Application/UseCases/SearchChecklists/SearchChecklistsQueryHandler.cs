using Application.DTOs.Checklist;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SearchChecklists;

public partial class SearchChecklistsQueryHandler(
    IChecklistRepository repository,
    ILogger<SearchChecklistsQueryHandler> logger)
{
    public SearchChecklistsResult Handle(SearchChecklistsQuery query)
    {
        LogSearchQuery(logger, query.SearchTerm ?? "empty");

        var items = repository.GetAll();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var normalizedSearch = query.SearchTerm.Trim();

            items = items
                .Where(c => c.Title.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                            c.Description.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        var results = items
            .Select(c => new ChecklistSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                UserId = c.UserId
            })
            .ToList();
        LogSearchResult(logger, results.Count);

        return new SearchChecklistsResult(results);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Search query: {SearchTerm}")]
    static partial void LogSearchQuery(ILogger logger, string searchTerm);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} results")]
    static partial void LogSearchResult(ILogger logger, int count);
}
