using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.RevokeChecklistAccess;

public sealed class RevokeChecklistAccessCommandHandler(
    IChecklistRepository repository,
    IChecklistReadOnlyRepository readRepository,
    ILogger<RevokeChecklistAccessCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(RevokeChecklistAccessCommand command)
    {
        logger.LogInformation(
            "User {OwnerId} is attempting to revoke access to checklist {ChecklistId} for user {TargetUserId}",
            command.OwnerId,
            command.ChecklistId,
            command.TargetUserId);

        var checklist = await readRepository.GetByIdAsync(command.ChecklistId);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {ChecklistId} not found", command.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to revoke access for checklist {ChecklistId} owned by {ActualOwner}",
                command.OwnerId,
                command.ChecklistId,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        await repository.RemoveAccessAsync(command.ChecklistId, command.TargetUserId);

        logger.LogInformation(
            "Access to checklist {ChecklistId} revoked successfully for user {TargetUserId}",
            command.ChecklistId,
            command.TargetUserId);

        return true;
    }
}
