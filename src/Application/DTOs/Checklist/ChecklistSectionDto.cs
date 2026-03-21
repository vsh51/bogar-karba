namespace Application.DTOs.Checklist;

public sealed class ChecklistSectionDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Position { get; init; }

    public IReadOnlyList<ChecklistItemDto> Items { get; init; } = Array.Empty<ChecklistItemDto>();
}
