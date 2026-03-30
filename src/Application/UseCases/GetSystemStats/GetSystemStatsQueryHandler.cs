using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Application.UseCases.GetSystemStats;

public sealed class GetSystemStatsQueryHandler
{
    private readonly IChecklistRepository _checklistRepository;
    private readonly IUserRepository _userRepository;

    public GetSystemStatsQueryHandler(
        IChecklistRepository checklistRepository,
        IUserRepository userRepository)
    {
        _checklistRepository = checklistRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<SystemStatsDto>> HandleAsync(GetSystemStatsQuery query)
    {
        _ = query;

        var totalChecklists = await _checklistRepository.GetTotalCountAsync();
        var totalUsers = await _userRepository.GetTotalCountAsync();

        return new SystemStatsDto(totalChecklists, totalUsers);
    }
}
