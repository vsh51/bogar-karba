namespace Application.UseCases.GetChecklistForEdit;

public sealed record GetChecklistForEditQuery(Guid ChecklistId, string OwnerId);
