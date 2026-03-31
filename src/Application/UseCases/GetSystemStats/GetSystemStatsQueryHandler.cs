using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetSystemStats;

public sealed class GetSystemStatsQueryHandler
{
    private readonly IChecklistRepository _checklistRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetSystemStatsQueryHandler> _logger;

    public GetSystemStatsQueryHandler(
        IChecklistRepository checklistRepository,
        IUserRepository userRepository,
        ILogger<GetSystemStatsQueryHandler> logger)
    {
        _checklistRepository = checklistRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<SystemStatsDto>> HandleAsync(GetSystemStatsQuery query)
    {
        _ = query;
        _logger.LogInformation("Fetching system statistics");

        var totalChecklists = await _checklistRepository.GetTotalCountAsync();
        var totalUsers = await _userRepository.GetTotalCountAsync();

        _logger.LogInformation(
            "System statistics fetched successfully: {TotalChecklists} checklists, {TotalUsers} users",
            totalChecklists,
            totalUsers);

        return new SystemStatsDto(totalChecklists, totalUsers);
    }
}
