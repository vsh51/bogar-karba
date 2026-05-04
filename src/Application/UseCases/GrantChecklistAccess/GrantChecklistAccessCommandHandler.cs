using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GrantChecklistAccess;

public sealed class GrantChecklistAccessCommandHandler(
    IChecklistReadOnlyRepository readRepository,
    IChecklistRepository repository,
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    ILogger<GrantChecklistAccessCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(GrantChecklistAccessCommand command)
    {
        logger.LogInformation(
            "Grant access to checklist {ChecklistId} for username '{Username}' requested by {OwnerId}",
            command.ChecklistId,
            command.Username,
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

        var targetUserId = await userRepository.GetUserIdByUsernameAsync(command.Username);

        if (targetUserId is null)
        {
            logger.LogWarning("User with username '{Username}' not found", command.Username);
            return ResultErrors.UserNotFound;
        }

        if (targetUserId == command.OwnerId)
        {
            logger.LogWarning("Owner {OwnerId} attempted to grant access to themselves", command.OwnerId);
            return ResultErrors.CannotGrantAccessToOwner;
        }

        var alreadyHasAccess = await readRepository.HasAccessAsync(command.ChecklistId, targetUserId);

        if (alreadyHasAccess)
        {
            logger.LogWarning(
                "User {TargetUserId} already has access to checklist {ChecklistId}",
                targetUserId,
                command.ChecklistId);
            return ResultErrors.UserAlreadyHasAccess;
        }

        await repository.GrantAccessAsync(new ChecklistAccess
        {
            ChecklistId = command.ChecklistId,
            UserId = targetUserId
        });

        await notificationRepository.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = targetUserId,
            Message = $"You have been granted access to the checklist \"{checklist.Title}\".",
            EventKey = $"access-granted:{command.ChecklistId}:{targetUserId}",
            CreatedAtUtc = DateTime.UtcNow
        });

        logger.LogInformation(
            "Access to checklist {ChecklistId} granted to user {TargetUserId}",
            command.ChecklistId,
            targetUserId);

        return true;
    }
}
