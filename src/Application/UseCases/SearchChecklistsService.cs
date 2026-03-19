using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases;

public partial class SearchChecklistsService(IChecklistRepository repository, ILogger<SearchChecklistsService> logger)
{
    public List<Checklist> Execute(string? searchTerm)
    {
        LogSearchQuery(logger, searchTerm ?? "empty");

        var query = repository.GetAll();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearch = searchTerm.Trim();

            query = query
                .AsEnumerable()
                .Where(c => c.Title.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                            c.Description.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .AsQueryable();
        }

        var results = query.ToList();
        LogSearchResult(logger, results.Count);

        return results;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Search query: {SearchTerm}")]
    static partial void LogSearchQuery(ILogger logger, string searchTerm);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} results")]
    static partial void LogSearchResult(ILogger logger, int count);
}
