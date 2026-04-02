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

        var checklist = await _repository.GetByIdWithSectionsAsync(query.Id, cancellationToken);

        if (checklist is null)
        {
            _logger.LogInformation("Checklist with id {ChecklistId} was not found", query.Id);
            return ResultErrors.ChecklistNotFound;
        }

        var isOwner = query.OwnerId is not null && checklist.UserId == query.OwnerId;

        if (!isOwner && checklist.Status != ChecklistStatus.Published)
        {
            _logger.LogInformation("Checklist {ChecklistId} is not published and user is not the owner", query.Id);
            return ResultErrors.ChecklistNotFound;
        }

        var result = checklist.ToPublishedChecklistResult();

        _logger.LogInformation("Checklist {ChecklistId} retrieved successfully", query.Id);

        return result;
    }
}
