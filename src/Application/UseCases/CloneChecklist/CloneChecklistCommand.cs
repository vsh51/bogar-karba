namespace Application.UseCases.CloneChecklist;

public sealed record CloneChecklistCommand(Guid SourceChecklistId, string OwnerId);
