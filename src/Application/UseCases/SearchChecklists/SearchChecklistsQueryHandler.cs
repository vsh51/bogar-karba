using Application.Common;
using Application.DTOs.Checklist;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SearchChecklists;

public class SearchChecklistsQueryHandler(
    IChecklistRepository repository,
    ILogger<SearchChecklistsQueryHandler> logger)
{
    public Result<List<ChecklistSummaryDto>> Handle(SearchChecklistsQuery query)
    {
        logger.LogInformation("Search query: {SearchTerm}", query.SearchTerm ?? "empty");

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

        logger.LogInformation("Found {Count} results", results.Count);

        return results;
    }
}
