namespace Web.Models.Checklist;

public sealed class ChecklistItemViewModel
{
    public Guid Id { get; init; }

    public string Content { get; init; } = string.Empty;

    public string? Link { get; init; }
}
