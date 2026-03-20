using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.DeleteChecklist;

public class DeleteChecklistCommandHandler
{
    private readonly IChecklistRepository _repository;
    private readonly ILogger<DeleteChecklistCommandHandler> _logger;

    public DeleteChecklistCommandHandler(IChecklistRepository repository, ILogger<DeleteChecklistCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DeleteChecklistResult> HandleAsync(DeleteChecklistCommand command)
    {
        _logger.LogInformation("Admin initiated checklist deletion, ID: {Id}", command.Id);

        try
        {
            await _repository.DeleteAsync(command.Id);
            _logger.LogInformation("Checklist {Id} deleted successfully", command.Id);
            return DeleteChecklistResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting checklist {Id}", command.Id);
            return DeleteChecklistResult.Failure($"Failed to delete checklist {command.Id}");
        }
    }
}
