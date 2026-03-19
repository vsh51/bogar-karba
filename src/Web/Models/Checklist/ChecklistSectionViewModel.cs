namespace Web.Models.Checklist;

public sealed class ChecklistSectionViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Position { get; init; }

    public IReadOnlyList<ChecklistItemViewModel> Items { get; init; } = Array.Empty<ChecklistItemViewModel>();
}
