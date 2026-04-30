namespace Application.UseCases.GetChecklistProgress;

public sealed record GetChecklistProgressQuery(Guid ChecklistId, string UserId);
