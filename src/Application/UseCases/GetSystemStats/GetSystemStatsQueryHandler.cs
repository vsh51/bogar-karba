using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetSystemStats;

public sealed class GetSystemStatsQueryHandler(
    IChecklistRepository checklistRepository,
    IUserRepository userRepository,
    ILogger<GetSystemStatsQueryHandler> logger)
{
    public async Task<Result<SystemStatsDto>> HandleAsync(GetSystemStatsQuery query)
    {
        _ = query;
        logger.LogInformation("Fetching system statistics");

        var totalChecklists = await checklistRepository.GetTotalCountAsync();
        var totalUsers = await userRepository.GetTotalCountAsync();

        logger.LogInformation(
            "System statistics fetched successfully: {TotalChecklists} checklists, {TotalUsers} users",
            totalChecklists,
            totalUsers);

        return new SystemStatsDto(totalChecklists, totalUsers);
    }
}
