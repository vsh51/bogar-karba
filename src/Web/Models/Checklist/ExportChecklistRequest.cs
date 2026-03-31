namespace Web.Models.Checklist;

public sealed class ExportChecklistRequest
{
    public IReadOnlyList<string> CompletedTaskIds { get; init; } = Array.Empty<string>();
}
