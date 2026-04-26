using Domain.Entities;

namespace Web.Models.Author;

public sealed class AuthorChecklistViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ChecklistStatus Status { get; init; }

    public bool IsPublic { get; init; }

    public DateOnly? Deadline { get; init; }

    public bool IsOutdated { get; init; }

    public string? DeadlineRemaining { get; init; }

    public bool IsActive => Status == ChecklistStatus.Published;
}
