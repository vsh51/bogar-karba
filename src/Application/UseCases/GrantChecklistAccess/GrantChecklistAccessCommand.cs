namespace Application.UseCases.GrantChecklistAccess;

public sealed record GrantChecklistAccessCommand(Guid ChecklistId, string OwnerId, string Username);
