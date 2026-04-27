namespace Application.UseCases.SaveChecklistProgress;

public sealed record SaveChecklistProgressCommand(
    Guid ChecklistId,
    string UserId,
    IReadOnlyList<Guid> CompletedTaskIds);
