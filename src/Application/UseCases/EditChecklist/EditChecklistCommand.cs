namespace Application.UseCases.EditChecklist;

public record EditChecklistCommand(
    Guid Id,
    string OwnerId,
    string Title,
    string Description,
    List<EditSectionRequest> Sections);
