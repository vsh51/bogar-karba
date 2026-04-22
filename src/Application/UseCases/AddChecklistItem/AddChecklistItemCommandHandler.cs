using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.AddChecklistItem;

public sealed class AddChecklistItemCommandHandler(
    IChecklistRepository repository,
    ILogger<AddChecklistItemCommandHandler> logger)
{
    public async Task<Result<Guid>> HandleAsync(AddChecklistItemCommand command)
    {
        logger.LogInformation(
            "Adding item to checklist {ChecklistId}, section {SectionId}, by user {OwnerId}",
            command.ChecklistId,
            command.SectionId,
            command.OwnerId);

        if (string.IsNullOrWhiteSpace(command.Content))
        {
            return "Item content is required.";
        }

        var checklist = await repository.GetByIdWithDetailsAsync(command.ChecklistId);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {ChecklistId} not found", command.ChecklistId);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to modify checklist {ChecklistId} owned by {ActualOwner}",
                command.OwnerId,
                command.ChecklistId,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        var section = checklist.Sections.FirstOrDefault(s => s.Id == command.SectionId);

        if (section is null)
        {
            return "Section not found.";
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Content = command.Content,
            SectionId = section.Id,
            Position = section.Tasks.Count,
            Link = string.IsNullOrWhiteSpace(command.Link) ? null : command.Link.Trim(),
        };

        section.Tasks.Add(task);
        await repository.AddTaskAsync(task);
        await repository.UpdateAsync();
        logger.LogInformation("Added item {TaskId} to section {SectionId}", task.Id, section.Id);
        return task.Id;
    }
}
