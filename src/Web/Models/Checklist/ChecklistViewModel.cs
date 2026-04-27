namespace Web.Models.Checklist;

public sealed class ChecklistViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateOnly? Deadline { get; init; }

    public bool IsPublic { get; init; }

    public bool IsOwner { get; init; }

    public bool IsOutdated { get; init; }

    public string? DeadlineRemaining { get; init; }

    public IReadOnlyList<string> InitialCompletedTaskIds { get; set; } = Array.Empty<string>();

    public IReadOnlyList<ChecklistSectionViewModel> Sections { get; init; } = Array.Empty<ChecklistSectionViewModel>();
}
