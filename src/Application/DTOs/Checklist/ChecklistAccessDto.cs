namespace Application.DTOs.Checklist;

public sealed class ChecklistAccessDto
{
    public string UserId { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public bool IsOwner { get; init; }
}
