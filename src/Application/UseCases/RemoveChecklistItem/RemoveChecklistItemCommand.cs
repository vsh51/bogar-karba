namespace Application.UseCases.RemoveChecklistItem;

public record RemoveChecklistItemCommand(
    Guid ChecklistId,
    string OwnerId,
    Guid TaskId);
