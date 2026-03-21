namespace Web.Models.Admin;

public sealed class AdminChecklistViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;
}
