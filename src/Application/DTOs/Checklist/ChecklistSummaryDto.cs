namespace Application.DTOs.Checklist;

public sealed class ChecklistSummaryDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public Domain.Entities.ChecklistStatus Status { get; init; }

    public DateOnly? Deadline { get; init; }

    public bool IsOutdated { get; init; }

    public string? DeadlineRemaining { get; init; }
}
