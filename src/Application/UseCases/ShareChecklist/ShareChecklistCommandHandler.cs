using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ShareChecklist;

public sealed class ShareChecklistCommandHandler(
    IChecklistRepository repository,
    IChecklistReadOnlyRepository readRepository,
    IUserRepository userRepository,
    ILogger<ShareChecklistCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(ShareChecklistCommand command)
    {
        logger.LogInformation(
            "User {OwnerId} is attempting to share checklist {ChecklistId} with user {TargetUsername}",
            command.OwnerId,
            command.ChecklistId,
            command.TargetUsername);

        var checklist = await readRepository.GetByIdAsync(command.ChecklistId);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {ChecklistId} not found", command.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to share checklist {ChecklistId} owned by {ActualOwner}",
                command.OwnerId,
                command.ChecklistId,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        if (checklist.IsPublic)
        {
            logger.LogWarning("Cannot share public checklist {ChecklistId}", command.ChecklistId);
            return ResultErrors.ChecklistNotPrivate;
        }

        var targetUserId = await userRepository.GetUserIdByUsernameAsync(command.TargetUsername);
        if (targetUserId is null)
        {
            logger.LogWarning("Target user {TargetUsername} not found", command.TargetUsername);
            return ResultErrors.TargetUserNotFound;
        }

        if (targetUserId == command.OwnerId)
        {
            logger.LogWarning("User {OwnerId} attempted to share checklist with themselves", command.OwnerId);
            return ResultErrors.CannotShareWithYourself;
        }

        var hasAccess = await readRepository.HasAccessAsync(command.ChecklistId, targetUserId);
        if (hasAccess)
        {
            logger.LogWarning("User {TargetUserId} already has access to checklist {ChecklistId}", targetUserId, command.ChecklistId);
            return ResultErrors.AlreadyHasAccess;
        }

        var access = new ChecklistAccess
        {
            ChecklistId = command.ChecklistId,
            UserId = targetUserId,
            IsOwner = false
        };

        await repository.AddAccessAsync(access);

        logger.LogInformation(
            "Checklist {ChecklistId} shared successfully with user {TargetUserId}",
            command.ChecklistId,
            targetUserId);

        return true;
    }
}
