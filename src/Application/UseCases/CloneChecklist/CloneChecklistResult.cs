namespace Application.UseCases.CloneChecklist;

public sealed class CloneChecklistResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public Guid? ClonedChecklistId { get; init; }

    public static CloneChecklistResult Success(Guid clonedChecklistId) =>
        new() { Succeeded = true, ClonedChecklistId = clonedChecklistId };

    public static CloneChecklistResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
