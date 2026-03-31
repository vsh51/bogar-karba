using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.CloneChecklist;

public sealed class CloneChecklistCommandHandler
{
    private readonly IChecklistRepository _repository;
    private readonly IChecklistReadOnlyRepository _readRepository;
    private readonly ILogger<CloneChecklistCommandHandler> _logger;

    public CloneChecklistCommandHandler(
        IChecklistRepository repository,
        IChecklistReadOnlyRepository readRepository,
        ILogger<CloneChecklistCommandHandler> logger)
    {
        _repository = repository;
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<CloneChecklistResult> HandleAsync(CloneChecklistCommand command)
    {
        _logger.LogInformation(
            "Initiated cloning of checklist {ChecklistId} for user {UserId}",
            command.SourceChecklistId,
            command.OwnerId);

        try
        {
            var sourceChecklist = await _readRepository.GetByIdWithDetailsAsync(command.SourceChecklistId);

            if (sourceChecklist is null)
            {
                _logger.LogWarning("Checklist {ChecklistId} not found for cloning", command.SourceChecklistId);
                return CloneChecklistResult.Failure("Checklist not found.");
            }

            if (sourceChecklist.UserId != command.OwnerId)
            {
                _logger.LogWarning(
                    "User {OwnerId} attempted to clone checklist {ChecklistId} owned by {ActualOwner}",
                    command.OwnerId,
                    sourceChecklist.Id,
                    sourceChecklist.UserId);
                return CloneChecklistResult.Failure("You can only clone your own checklists.");
            }

            var clonedChecklist = new Checklist
            {
                Id = Guid.NewGuid(),
                Title = BuildCopyTitle(sourceChecklist.Title),
                Description = sourceChecklist.Description,
                Status = ChecklistStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UserId = sourceChecklist.UserId,
                Sections = sourceChecklist.Sections
                    .OrderBy(section => section.Position)
                    .Select(section => new Section
                    {
                        Id = Guid.NewGuid(),
                        Name = section.Name,
                        Position = section.Position,
                        Tasks = section.Tasks
                            .OrderBy(task => task.Position)
                            .Select(task => new TaskItem
                            {
                                Id = Guid.NewGuid(),
                                Content = task.Content,
                                Position = task.Position
                            })
                            .ToList()
                    })
                    .ToList()
            };

            await _repository.AddAsync(clonedChecklist);

            _logger.LogInformation(
                "Checklist {SourceChecklistId} cloned to {ClonedChecklistId}",
                command.SourceChecklistId,
                clonedChecklist.Id);

            return CloneChecklistResult.Success(clonedChecklist.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning checklist {ChecklistId}", command.SourceChecklistId);
            return CloneChecklistResult.Failure("An error occurred while cloning the checklist.");
        }
    }

    private static string BuildCopyTitle(string title)
    {
        return string.IsNullOrWhiteSpace(title)
            ? "(Copy)"
            : $"{title} (Copy)";
    }
}
