using System.Globalization;
using System.Text;
using Application.Common;
using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UseCases.QuickCreateChecklist;

public sealed class QuickCreateChecklistCommandHandler(
    IChecklistRepository repository,
    IOptions<ChecklistOptions> options,
    ILogger<QuickCreateChecklistCommandHandler> logger)
{
    private static readonly CompositeFormat TitleTooLongFormat =
        CompositeFormat.Parse(QuickCreateErrors.TitleTooLong);

    private static readonly CompositeFormat SectionNameTooLongFormat =
        CompositeFormat.Parse(QuickCreateErrors.SectionNameTooLong);

    public async Task<Result<Guid>> HandleAsync(QuickCreateChecklistCommand request, string userId)
    {
        logger.LogInformation(
            "Quick checklist creation requested by user {UserId}: input length {Length}",
            userId,
            request.RawText?.Length ?? 0);

        var parseResult = QuickCreateChecklistParser.Parse(request.RawText);
        if (!parseResult.Succeeded)
        {
            logger.LogWarning(
                "Quick checklist parsing failed for user {UserId}: {Error}",
                userId,
                parseResult.ErrorMessage);
            return parseResult.ErrorMessage!;
        }

        var parsed = parseResult.Value!;
        var limits = options.Value;

        if (parsed.Title.Length > limits.TitleMaxLength)
        {
            return string.Format(
                CultureInfo.InvariantCulture, TitleTooLongFormat, limits.TitleMaxLength);
        }

        var totalTasks = 0;
        foreach (var section in parsed.Sections)
        {
            if (section.Name.Length > limits.SectionNameMaxLength)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    SectionNameTooLongFormat,
                    limits.SectionNameMaxLength);
            }

            totalTasks += section.Tasks.Count;
        }

        if (totalTasks == 0)
        {
            logger.LogWarning("Quick checklist creation failed for user {UserId}: no tasks", userId);
            return QuickCreateErrors.NoTasks;
        }

        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            Title = parsed.Title,
            Description = parsed.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = ChecklistStatus.Published,
            Sections = parsed.Sections.Select(s => new Section
            {
                Id = Guid.NewGuid(),
                Name = s.Name,
                Position = s.Position,
                Tasks = s.Tasks.Select((content, index) => new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Content = content,
                    Position = index
                }).ToList()
            }).ToList()
        };

        await repository.AddAsync(checklist);

        logger.LogInformation(
            "Successfully quick-created checklist {ChecklistId} for user {UserId} with {SectionCount} sections, {TaskCount} tasks",
            checklist.Id,
            userId,
            checklist.Sections.Count,
            totalTasks);

        return checklist.Id;
    }
}
