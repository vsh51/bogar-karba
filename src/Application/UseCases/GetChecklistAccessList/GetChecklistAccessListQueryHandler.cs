using Application.Common;
using Application.DTOs.Checklist;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetChecklistAccessList;

public sealed class GetChecklistAccessListQueryHandler(
    IChecklistReadOnlyRepository readRepository,
    IUserRepository userRepository,
    ILogger<GetChecklistAccessListQueryHandler> logger)
{
    public async Task<Result<List<ChecklistAccessUserDto>>> HandleAsync(
        GetChecklistAccessListQuery query,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Access list for checklist {ChecklistId} requested by {OwnerId}",
            query.ChecklistId,
            query.OwnerId);

        var checklist = await readRepository.GetByIdAsync(query.ChecklistId);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {ChecklistId} not found", query.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != query.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} is not the owner of checklist {ChecklistId}",
                query.OwnerId,
                query.ChecklistId);
            return ResultErrors.NotChecklistOwner;
        }

        var userIds = await readRepository.GetAccessUserIdsAsync(query.ChecklistId, cancellationToken);
        var usernameMap = await userRepository.GetUsernamesByIdsAsync(userIds);

        var result = userIds
            .Select(id => new ChecklistAccessUserDto
            {
                UserId = id,
                UserName = usernameMap.GetValueOrDefault(id, id)
            })
            .ToList();

        logger.LogInformation(
            "Access list for checklist {ChecklistId} returned {Count} entries",
            query.ChecklistId,
            result.Count);

        return result;
    }
}
