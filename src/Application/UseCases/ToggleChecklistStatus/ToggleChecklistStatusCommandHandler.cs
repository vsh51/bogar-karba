using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ToggleChecklistStatus;

public class ToggleChecklistStatusCommandHandler
{
    private readonly IChecklistRepository _repository;
    private readonly IChecklistReadOnlyRepository _readRepository;
    private readonly ILogger<ToggleChecklistStatusCommandHandler> _logger;

    public ToggleChecklistStatusCommandHandler(
        IChecklistRepository repository,
        IChecklistReadOnlyRepository readRepository,
        ILogger<ToggleChecklistStatusCommandHandler> logger)
    {
        _repository = repository;
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(ToggleChecklistStatusCommand command)
    {
        _logger.LogInformation("Initiated status change of checklist {Id} to {NewStatus}", command.Id, command.NewStatus);

        var checklist = await _readRepository.GetByIdAsync(command.Id);

        if (checklist is null)
        {
            _logger.LogWarning("Checklist {Id} not found", command.Id);
            return ResultErrors.ChecklistNotFound;
        }

        if (command.OwnerId is not null && checklist.UserId != command.OwnerId)
        {
            _logger.LogWarning(
                "User {OwnerId} attempted to change status of checklist {Id} owned by {ActualOwner}",
                command.OwnerId,
                command.Id,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        await _repository.UpdateStatusAsync(command.Id, command.NewStatus);
        _logger.LogInformation("Checklist {Id} status changed to {NewStatus}", command.Id, command.NewStatus);
        return true;
    }
}
