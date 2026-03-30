using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.CreateChecklist;

public class CreateChecklistCommandHandler(
    IChecklistRepository repository,
    ILogger<CreateChecklistCommandHandler> logger)
{
    public async Task<CreateChecklistResult> HandleAsync(CreateChecklistCommand request, string userId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return CreateChecklistResult.Failure();
        }

        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = ChecklistStatus.Published,
            Sections = request.Sections.Select(s => new Section
            {
                Id = Guid.NewGuid(),
                Name = s.Name,
                Position = s.Position,
                Tasks = s.Tasks.Select(t => new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Content = t.Content,
                    Position = t.Position
                }).ToList()
            }).ToList()
        };

        try
        {
            await repository.AddAsync(checklist);
            logger.LogInformation("Successfully created checklist {ChecklistId} for user {UserId}", checklist.Id, userId);
            return CreateChecklistResult.Success(checklist.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create checklist for user {UserId}", userId);
            return CreateChecklistResult.Failure();
        }
    }
}
