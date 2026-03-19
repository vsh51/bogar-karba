using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ChecklistService
{
    private readonly IChecklistRepository _repository;
    private readonly ILogger<ChecklistService> _logger;

    public ChecklistService(IChecklistRepository repository, ILogger<ChecklistService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task DeleteChecklist(Guid id)
    {
        _logger.LogInformation("Admin initiated checklist deletion, ID: {Id}", id);

        try
        {
            await _repository.DeleteAsync(id);
            _logger.LogInformation("Checklist {Id} deleted successfully", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting checklist {Id}", id);
            throw new InvalidOperationException($"Failed to delete checklist {id}", ex);
        }
    }
}
