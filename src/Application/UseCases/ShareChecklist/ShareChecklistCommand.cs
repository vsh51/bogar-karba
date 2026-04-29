namespace Application.UseCases.ShareChecklist;

public sealed record ShareChecklistCommand(
    Guid ChecklistId,
    string OwnerId,
    string TargetUsername);
