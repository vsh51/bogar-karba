using Application.Common;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetPublishedChecklist;

public sealed class GetPublishedChecklistQueryHandler
{
    private readonly IChecklistReadOnlyRepository _repository;
    private readonly ILogger<GetPublishedChecklistQueryHandler> _logger;

    public GetPublishedChecklistQueryHandler(
        IChecklistReadOnlyRepository repository,
        ILogger<GetPublishedChecklistQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<GetPublishedChecklistResult>> HandleAsync(
        GetPublishedChecklistQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching published checklist {ChecklistId}", query.Id);

        var checklist = await _repository.GetPublishedChecklistAsync(
            query.Id, cancellationToken);

        if (checklist is null)
        {
            _logger.LogWarning("Checklist {ChecklistId} was not found or not published", query.Id);
            return Result<GetPublishedChecklistResult>.Failure("Checklist not found or not published.");
        }

        var result = checklist.ToPublishedChecklistResult();

        _logger.LogInformation("Checklist {ChecklistId} retrieved successfully", query.Id);

        return result;
    }
}
