using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.EditChecklist;

public class EditChecklistCommandHandler(
    IChecklistRepository repository,
    ILogger<EditChecklistCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(EditChecklistCommand command)
    {
        logger.LogInformation("Initiated editing of checklist {Id} by user {OwnerId}", command.Id, command.OwnerId);

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return "Title is required.";
        }

        var checklist = await repository.GetByIdWithDetailsAsync(command.Id);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {Id} not found", command.Id);
            return "Checklist not found.";
        }

        if (checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to edit checklist {Id} owned by {ActualOwner}",
                command.OwnerId,
                command.Id,
                checklist.UserId);
            return "You can only edit your own checklists.";
        }

        var existingSectionIds = checklist.Sections.Select(s => s.Id).ToHashSet();
        foreach (var sectionRequest in command.Sections)
        {
            if (!existingSectionIds.Contains(sectionRequest.Id))
            {
                return "Adding new sections is not allowed.";
            }

            var existingTaskIds = checklist.Sections
                .First(s => s.Id == sectionRequest.Id)
                .Tasks.Select(t => t.Id).ToHashSet();

            if (sectionRequest.Tasks.Any(t => !existingTaskIds.Contains(t.Id)))
            {
                return "Adding new tasks is not allowed.";
            }
        }

        checklist.Title = command.Title;
        checklist.Description = command.Description;

        var requestedSectionIds = command.Sections.Select(s => s.Id).ToHashSet();
        var sectionsToRemove = checklist.Sections
            .Where(s => !requestedSectionIds.Contains(s.Id))
            .ToList();
        foreach (var section in sectionsToRemove)
        {
            checklist.Sections.Remove(section);
        }

        foreach (var sectionRequest in command.Sections)
        {
            var section = checklist.Sections.First(s => s.Id == sectionRequest.Id);
            section.Name = sectionRequest.Name;

            var requestedTaskIds = sectionRequest.Tasks.Select(t => t.Id).ToHashSet();
            var tasksToRemove = section.Tasks
                .Where(t => !requestedTaskIds.Contains(t.Id))
                .ToList();
            foreach (var task in tasksToRemove)
            {
                section.Tasks.Remove(task);
            }

            foreach (var taskRequest in sectionRequest.Tasks)
            {
                var task = section.Tasks.First(t => t.Id == taskRequest.Id);
                task.Content = taskRequest.Content;
            }
        }

        await repository.UpdateAsync();
        logger.LogInformation("Successfully updated checklist {ChecklistId} for user {UserId}", checklist.Id, command.OwnerId);
        return true;
    }
}
