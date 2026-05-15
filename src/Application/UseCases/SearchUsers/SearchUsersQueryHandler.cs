using Application.Common;
using Application.DTOs.User;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SearchUsers;

public sealed class SearchUsersQueryHandler(
    IUserRepository userRepository,
    ILogger<SearchUsersQueryHandler> logger)
{
    public async Task<Result<List<UserSummaryDto>>> HandleAsync(SearchUsersQuery query)
    {
        logger.LogInformation("Admin user search requested with term: {SearchTerm}", query.SearchTerm ?? "<empty>");

        var users = await userRepository.SearchUsersAsync(query.SearchTerm);

        logger.LogInformation("Search completed. Found {Count} users", users.Count);

        return users;
    }
}
