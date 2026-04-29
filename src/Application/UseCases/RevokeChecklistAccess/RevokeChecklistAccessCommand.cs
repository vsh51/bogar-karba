namespace Application.UseCases.RevokeChecklistAccess;

public sealed record RevokeChecklistAccessCommand(
    Guid ChecklistId,
    string OwnerId,
    string TargetUserId);
