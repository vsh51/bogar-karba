using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SetChecklistEmbeddable;

public sealed class SetChecklistEmbeddableCommandHandler(
    IChecklistRepository repository,
    IChecklistReadOnlyRepository readRepository,
    ILogger<SetChecklistEmbeddableCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(SetChecklistEmbeddableCommand command)
    {
        logger.LogInformation(
            "Initiated embeddable change of checklist {Id} to {Embeddable}",
            command.Id,
            command.IsEmbeddable ? "enabled" : "disabled");

        var checklist = await readRepository.GetByIdAsync(command.Id);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {Id} not found", command.Id);
            return ResultErrors.ChecklistNotFound;
        }

        if (command.OwnerId is not null && checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to change embed setting of checklist {Id} owned by {ActualOwner}",
                command.OwnerId,
                command.Id,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        await repository.UpdateEmbeddableAsync(command.Id, command.IsEmbeddable);
        logger.LogInformation(
            "Checklist {Id} embeddable changed to {Embeddable}",
            command.Id,
            command.IsEmbeddable ? "enabled" : "disabled");
        return true;
    }
}
