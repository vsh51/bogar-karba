namespace Application.UseCases.AddChecklistItem;

public record AddChecklistItemCommand(
    Guid ChecklistId,
    string OwnerId,
    Guid SectionId,
    string Content,
    string? Link = null);
