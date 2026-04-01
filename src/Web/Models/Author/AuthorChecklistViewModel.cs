using Domain.Entities;

namespace Web.Models.Author;

public sealed class AuthorChecklistViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ChecklistStatus Status { get; init; }
}
