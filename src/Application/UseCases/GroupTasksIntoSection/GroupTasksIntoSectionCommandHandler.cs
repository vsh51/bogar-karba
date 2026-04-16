using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GroupTasksIntoSection;

public sealed class GroupTasksIntoSectionCommandHandler(
    IChecklistRepository repository,
    ILogger<GroupTasksIntoSectionCommandHandler> logger)
{
    public async Task<Result<Guid>> HandleAsync(GroupTasksIntoSectionCommand command)
    {
        logger.LogInformation(
            "Grouping {Count} tasks into new section '{SectionName}' in checklist {ChecklistId} by user {OwnerId}",
            command.TaskIds.Count,
            command.SectionName,
            command.ChecklistId,
            command.OwnerId);

        if (string.IsNullOrWhiteSpace(command.SectionName))
        {
            return "Section name is required.";
        }

        if (command.TaskIds.Count == 0)
        {
            return "At least one item must be selected.";
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

        var requestedIds = command.TaskIds.ToHashSet();
        var tasksBySection = checklist.Sections
            .SelectMany(s => s.Tasks.Where(t => requestedIds.Contains(t.Id)).Select(t => (Section: s, Task: t)))
            .ToList();

        if (tasksBySection.Count != requestedIds.Count)
        {
            return "Some items do not belong to this checklist.";
        }

        var newSection = new Section
        {
            Id = Guid.NewGuid(),
            Name = command.SectionName,
            ChecklistId = checklist.Id,
            Position = checklist.Sections.Count,
            Tasks = new List<TaskItem>(),
        };

        checklist.Sections.Add(newSection);
        await repository.AddSectionAsync(newSection);

        var orderedTasks = command.TaskIds
            .Select(id => tasksBySection.First(x => x.Task.Id == id))
            .ToList();

        foreach (var (source, task) in orderedTasks)
        {
            source.Tasks.Remove(task);
        }

        for (var i = 0; i < orderedTasks.Count; i++)
        {
            var task = orderedTasks[i].Task;
            task.SectionId = newSection.Id;
            task.Position = i;
            newSection.Tasks.Add(task);
        }

        var affectedSources = orderedTasks.Select(x => x.Section).Distinct().ToList();
        foreach (var source in affectedSources)
        {
            Resequence(source.Tasks);
        }

        await repository.UpdateAsync();
        logger.LogInformation(
            "Grouped {Count} tasks into section {SectionId} in checklist {ChecklistId}",
            orderedTasks.Count,
            newSection.Id,
            checklist.Id);
        return newSection.Id;
    }

    private static void Resequence(List<TaskItem> tasks)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            tasks[i].Position = i;
        }
    }
}
