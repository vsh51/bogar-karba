using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.DeleteChecklist;

public sealed class DeleteChecklistCommandHandler(
    IChecklistRepository repository,
    IChecklistReadOnlyRepository readRepository,
    ILogger<DeleteChecklistCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(DeleteChecklistCommand command)
    {
        logger.LogInformation("Initiated deletion of checklist {Id}", command.Id);

        if (command.OwnerId is not null)
        {
            var checklist = await readRepository.GetByIdAsync(command.Id);

            if (checklist is null)
            {
                logger.LogWarning("Checklist {Id} not found", command.Id);
                return "Checklist not found.";
            }

            if (checklist.UserId != command.OwnerId)
            {
                logger.LogWarning(
                    "User {OwnerId} attempted to delete checklist {Id} owned by {ActualOwner}",
                    command.OwnerId,
                    command.Id,
                    checklist.UserId);
                return "You can only delete your own checklists.";
            }
        }

        await repository.DeleteAsync(command.Id);
        logger.LogInformation("Checklist {Id} deleted successfully", command.Id);
        return true;
    }
}
