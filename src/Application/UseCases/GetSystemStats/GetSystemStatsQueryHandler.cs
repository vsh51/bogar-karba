using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
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
        var publishedChecklists = await checklistRepository.GetCountByStatusAsync(ChecklistStatus.Published);
        var draftChecklists = await checklistRepository.GetCountByStatusAsync(ChecklistStatus.Draft);
        var archivedChecklists = await checklistRepository.GetCountByStatusAsync(ChecklistStatus.Archived);

        logger.LogInformation(
            "System statistics fetched successfully: {TotalChecklists} checklists, {TotalUsers} users, {PublishedChecklists} published, {DraftChecklists} drafts, {ArchivedChecklists} archived",
            totalChecklists,
            totalUsers,
            publishedChecklists,
            draftChecklists,
            archivedChecklists);

        return new SystemStatsDto(
            totalChecklists,
            totalUsers,
            publishedChecklists,
            draftChecklists,
            archivedChecklists);
    }
}
