namespace Web.Models.Checklist;

public sealed class SaveChecklistProgressRequest
{
    public IReadOnlyList<string> CompletedTaskIds { get; init; } = Array.Empty<string>();
}
