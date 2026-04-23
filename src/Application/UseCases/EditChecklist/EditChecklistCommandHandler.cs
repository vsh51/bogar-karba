using Application.Common;
using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UseCases.EditChecklist;

public sealed class EditChecklistCommandHandler(
    IChecklistRepository repository,
    IOptions<ChecklistOptions> options,
    ILogger<EditChecklistCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(EditChecklistCommand command)
    {
        logger.LogInformation("Initiated editing of checklist {Id} by user {OwnerId}", command.Id, command.OwnerId);

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return ResultErrors.TitleRequired;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deadlineError = DeadlineValidator.Validate(
            command.Deadline, today, options.Value.MaxDeadlineYears);
        if (deadlineError is not null)
        {
            return deadlineError;
        }

        var checklist = await repository.GetByIdWithDetailsAsync(command.Id);

        if (checklist is null)
        {
            logger.LogWarning("Checklist {Id} not found", command.Id);
            return ResultErrors.ChecklistNotFound;
        }

        if (checklist.UserId != command.OwnerId)
        {
            logger.LogWarning(
                "User {OwnerId} attempted to edit checklist {Id} owned by {ActualOwner}",
                command.OwnerId,
                command.Id,
                checklist.UserId);
            return ResultErrors.NotChecklistOwner;
        }

        var existingSectionIds = checklist.Sections.Select(s => s.Id).ToHashSet();
        foreach (var sectionRequest in command.Sections)
        {
            if (!existingSectionIds.Contains(sectionRequest.Id))
            {
                return ResultErrors.AddingSectionsNotAllowed;
            }

            var existingTaskIds = checklist.Sections
                .First(s => s.Id == sectionRequest.Id)
                .Tasks.Select(t => t.Id).ToHashSet();

            if (sectionRequest.Tasks.Any(t => !existingTaskIds.Contains(t.Id)))
            {
                return ResultErrors.AddingTasksNotAllowed;
            }
        }

        checklist.Title = command.Title;
        checklist.Description = command.Description;
        checklist.Deadline = command.Deadline;

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
                task.Link = string.IsNullOrWhiteSpace(taskRequest.Link) ? null : taskRequest.Link.Trim();
            }
        }

        await repository.UpdateAsync();
        logger.LogInformation("Successfully updated checklist {ChecklistId} for user {UserId}", checklist.Id, command.OwnerId);
        return true;
    }
}
