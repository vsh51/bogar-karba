namespace Application.UseCases.DeleteAuthorChecklist;

public sealed record DeleteAuthorChecklistCommand(Guid ChecklistId, string UserId);
