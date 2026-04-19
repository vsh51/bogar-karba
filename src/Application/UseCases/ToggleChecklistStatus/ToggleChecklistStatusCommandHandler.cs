using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ToggleChecklistStatus;

public sealed class ToggleChecklistStatusCommandHandler(
    IChecklistRepository repository,
    IChecklistReadOnlyRepository readRepository,
    ILogger<ToggleChecklistStatusCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(ToggleChecklistStatusCommand command)
    {
        logger.LogInformation("Initiated status change of checklist {Id} to {NewStatus}", command.Id, command.NewStatus);

        var checklist = await readRepository.GetByIdAsync(command.Id);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {Id} not found", command.Id);
            return ResultErrors.ChecklistNotFound;
        }

        if (command.OwnerId is not null && checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to change status of checklist {Id} owned by {ActualOwner}",
                command.OwnerId,
                command.Id,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        await repository.UpdateStatusAsync(command.Id, command.NewStatus);
        logger.LogInformation("Checklist {Id} status changed to {NewStatus}", command.Id, command.NewStatus);
        return true;
    }
}
