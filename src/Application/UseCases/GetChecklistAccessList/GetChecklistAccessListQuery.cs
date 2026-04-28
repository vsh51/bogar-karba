namespace Application.UseCases.GetChecklistAccessList;

public sealed record GetChecklistAccessListQuery(Guid ChecklistId, string OwnerId);
