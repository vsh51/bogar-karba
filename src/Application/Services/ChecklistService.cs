using Application.Common.Interfaces;
using Domain.Entities;
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

    public async Task<IEnumerable<Checklist>> GetAllChecklists()
    {
        _logger.LogInformation("Запит на отримання всіх чеклистів для адмін-панелі");
        return await _repository.GetAllAsync();
    }

    public async Task DeleteChecklist(Guid id)
    {
        _logger.LogInformation("Адмін ініціював видалення чеклиста з ID: {Id}", id);

        try
        {
            await _repository.DeleteAsync(id);
            _logger.LogInformation("Чеклист {Id} успішно видалено", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при видаленні чеклиста {Id}", id);
            throw new InvalidOperationException($"Не вдалося видалити чеклист {id}", ex);
        }
    }
}