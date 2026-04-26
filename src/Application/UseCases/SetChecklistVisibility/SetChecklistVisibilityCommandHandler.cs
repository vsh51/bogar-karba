using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SetChecklistVisibility;

public sealed class SetChecklistVisibilityCommandHandler(
    IChecklistRepository repository,
    IChecklistReadOnlyRepository readRepository,
    ILogger<SetChecklistVisibilityCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(SetChecklistVisibilityCommand command)
    {
        logger.LogInformation("Initiated visibility change of checklist {Id} to {Visibility}", command.Id, command.IsPublic ? "public" : "private");

        var checklist = await readRepository.GetByIdAsync(command.Id);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {Id} not found", command.Id);
            return ResultErrors.ChecklistNotFound;
        }

        if (command.OwnerId is not null && checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to change visibility of checklist {Id} owned by {ActualOwner}",
                command.OwnerId,
                command.Id,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        await repository.UpdateVisibilityAsync(command.Id, command.IsPublic);
        logger.LogInformation("Checklist {Id} visibility changed to {Visibility}", command.Id, command.IsPublic ? "public" : "private");
        return true;
    }
}
