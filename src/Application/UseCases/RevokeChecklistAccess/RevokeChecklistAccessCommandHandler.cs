using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.RevokeChecklistAccess;

public sealed class RevokeChecklistAccessCommandHandler(
    IChecklistReadOnlyRepository readRepository,
    IChecklistRepository repository,
    INotificationRepository notificationRepository,
    ILogger<RevokeChecklistAccessCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(RevokeChecklistAccessCommand command)
    {
        logger.LogInformation(
            "Revoke access to checklist {ChecklistId} for user {TargetUserId} requested by {OwnerId}",
            command.ChecklistId,
            command.TargetUserId,
            command.OwnerId);

        var checklist = await readRepository.GetByIdAsync(command.ChecklistId);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {ChecklistId} not found", command.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} is not the owner of checklist {ChecklistId}",
                command.OwnerId,
                command.ChecklistId);
            return ResultErrors.NotChecklistOwner;
        }

        await repository.RevokeAccessAsync(command.ChecklistId, command.TargetUserId);

        await notificationRepository.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = command.TargetUserId,
            Message = $"Your access to the checklist \"{checklist.Title}\" has been removed.",
            EventKey = $"access-revoked:{command.ChecklistId}:{command.TargetUserId}:{DateTime.UtcNow:yyyyMMddHHmmss}",
            CreatedAtUtc = DateTime.UtcNow
        });

        logger.LogInformation(
            "Access to checklist {ChecklistId} revoked for user {TargetUserId}",
            command.ChecklistId,
            command.TargetUserId);

        return true;
    }
}
