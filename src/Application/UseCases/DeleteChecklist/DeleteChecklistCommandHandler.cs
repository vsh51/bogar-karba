using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.DeleteChecklist;

public class DeleteChecklistCommandHandler
{
    private readonly IChecklistRepository _repository;
    private readonly IChecklistReadOnlyRepository _readRepository;
    private readonly ILogger<DeleteChecklistCommandHandler> _logger;

    public DeleteChecklistCommandHandler(
        IChecklistRepository repository,
        IChecklistReadOnlyRepository readRepository,
        ILogger<DeleteChecklistCommandHandler> logger)
    {
        _repository = repository;
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(DeleteChecklistCommand command)
    {
        _logger.LogInformation("Initiated deletion of checklist {Id}", command.Id);

        if (command.OwnerId is not null)
        {
            var checklist = await _readRepository.GetByIdAsync(command.Id);

            if (checklist is null)
            {
                _logger.LogWarning("Checklist {Id} not found", command.Id);
                return Result<bool>.Failure("Checklist not found.");
            }

            if (checklist.UserId != command.OwnerId)
            {
                _logger.LogWarning(
                    "User {OwnerId} attempted to delete checklist {Id} owned by {ActualOwner}",
                    command.OwnerId,
                    command.Id,
                    checklist.UserId);
                return Result<bool>.Failure("You can only delete your own checklists.");
            }
        }

        await _repository.DeleteAsync(command.Id);
        _logger.LogInformation("Checklist {Id} deleted successfully", command.Id);
        return Result<bool>.Success(true);
    }
}
