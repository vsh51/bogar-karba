using System.Text;
using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ExportChecklist.Markdown;

public sealed class ExportMarkdownQueryHandler
{
    private readonly IChecklistReadOnlyRepository _repository;
    private readonly ILogger<ExportMarkdownQueryHandler> _logger;

    public ExportMarkdownQueryHandler(
        IChecklistReadOnlyRepository repository,
        ILogger<ExportMarkdownQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ExportChecklistResult>> HandleAsync(
        ExportChecklistQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling ExportMarkdownQuery for ChecklistId: {ChecklistId}",
            query.ChecklistId);

        var checklist = await _repository.GetPublishedChecklistAsync(
            query.ChecklistId, cancellationToken);

        if (checklist is null)
        {
            _logger.LogInformation(
                "Checklist with id {ChecklistId} was not found or not published",
                query.ChecklistId);
            return Result<ExportChecklistResult>.Failure("Checklist not found or not published.");
        }

        var completedSet = new HashSet<Guid>(query.CompletedTaskIds);
        string markdown = BuildMarkdown(checklist, completedSet);

        _logger.LogInformation(
            "Successfully exported markdown for ChecklistId: {ChecklistId}",
            query.ChecklistId);

        return Result<ExportChecklistResult>.Success(new ExportChecklistResult { Content = markdown });
    }

    private static string BuildMarkdown(Checklist checklist, HashSet<Guid> completedTaskIds)
    {
        var sb = new StringBuilder();

        sb.Append("# ").AppendLine(checklist.Title);

        if (!string.IsNullOrWhiteSpace(checklist.Description))
        {
            sb.AppendLine();
            sb.AppendLine(checklist.Description);
        }

        foreach (var section in checklist.Sections.OrderBy(s => s.Position))
        {
            sb.AppendLine();
            sb.Append("## ").AppendLine(section.Name);
            sb.AppendLine();

            foreach (var task in section.Tasks.OrderBy(t => t.Position))
            {
                string marker = completedTaskIds.Contains(task.Id) ? "[+]" : "[ ]";
                sb.Append("- ").Append(marker).Append(' ').AppendLine(task.Content);
            }
        }

        return sb.ToString().TrimEnd();
    }
}
