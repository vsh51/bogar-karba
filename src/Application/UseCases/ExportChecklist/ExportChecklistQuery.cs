namespace Application.UseCases.ExportChecklist;

public sealed record ExportChecklistQuery(
    Guid ChecklistId,
    IReadOnlyList<Guid> CompletedTaskIds);
