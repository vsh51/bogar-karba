using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.DeleteAuthorChecklist;

public class DeleteAuthorChecklistCommandHandler
{
    private readonly IChecklistRepository _repository;
    private readonly ILogger<DeleteAuthorChecklistCommandHandler> _logger;

    public DeleteAuthorChecklistCommandHandler(
        IChecklistRepository repository,
        ILogger<DeleteAuthorChecklistCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DeleteAuthorChecklistResult> HandleAsync(DeleteAuthorChecklistCommand command)
    {
        _logger.LogInformation(
            "User {UserId} initiated deletion of checklist {ChecklistId}",
            command.UserId,
            command.ChecklistId);

        try
        {
            var checklist = await _repository.GetByIdAsync(command.ChecklistId);

            if (checklist is null)
            {
                _logger.LogWarning("Checklist {ChecklistId} not found", command.ChecklistId);
                return DeleteAuthorChecklistResult.Failure("Checklist not found.");
            }

            if (checklist.UserId != command.UserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete checklist {ChecklistId} owned by {OwnerId}",
                    command.UserId,
                    command.ChecklistId,
                    checklist.UserId);
                return DeleteAuthorChecklistResult.Failure("You can only delete your own checklists.");
            }

            await _repository.DeleteAsync(command.ChecklistId);

            _logger.LogInformation(
                "Checklist {ChecklistId} deleted successfully by user {UserId}",
                command.ChecklistId,
                command.UserId);

            return DeleteAuthorChecklistResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting checklist {ChecklistId}", command.ChecklistId);
            return DeleteAuthorChecklistResult.Failure($"Failed to delete checklist.");
        }
    }
}
