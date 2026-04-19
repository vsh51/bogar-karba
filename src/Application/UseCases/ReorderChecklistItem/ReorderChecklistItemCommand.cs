namespace Application.UseCases.ReorderChecklistItem;

public record ReorderChecklistItemCommand(
    Guid ChecklistId,
    string OwnerId,
    Guid TaskId,
    Guid TargetSectionId,
    int NewPosition);
