namespace Application.UseCases.SetChecklistVisibility;

public sealed record SetChecklistVisibilityCommand(Guid Id, bool IsPublic, string? OwnerId = null);
