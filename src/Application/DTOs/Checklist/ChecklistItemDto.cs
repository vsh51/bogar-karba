namespace Application.DTOs.Checklist;

public sealed class ChecklistItemDto
{
    public Guid Id { get; init; }

    public string Content { get; init; } = string.Empty;

    public int Position { get; init; }

    public string? Link { get; init; }
}
