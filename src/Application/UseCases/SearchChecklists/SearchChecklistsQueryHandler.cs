using Application.Common;
using Application.DTOs.Checklist;
using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UseCases.SearchChecklists;

public sealed class SearchChecklistsQueryHandler(
    IChecklistRepository repository,
    IUserRepository userRepository,
    IOptions<ChecklistOptions> options,
    ILogger<SearchChecklistsQueryHandler> logger)
{
    public async Task<Result<List<ChecklistSummaryDto>>> HandleAsync(SearchChecklistsQuery query)
    {
        logger.LogInformation("Search query: {SearchTerm}", query.SearchTerm ?? "empty");

        var items = await repository.GetAllAsync();

        IEnumerable<Checklist> filtered = items;
        var searchTerm = query.SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length >= options.Value.SearchMinLength)
        {
            filtered = items.Where(c =>
                c.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = filtered.ToList();

        var userIds = filteredList.Select(c => c.UserId).Distinct();
        var usernames = await userRepository.GetUsernamesByIdsAsync(userIds);

        var results = filteredList
            .Select(c => new ChecklistSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                UserId = c.UserId,
                UserName = usernames.GetValueOrDefault(c.UserId, c.UserId),
                Status = c.Status
            })
            .ToList();

        logger.LogInformation("Found {Count} results", results.Count);

        return results;
    }
}
