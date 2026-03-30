namespace Application.UseCases.CreateChecklist;

public record CreateChecklistCommand(
    string Title,
    string Description,
    List<CreateSectionRequest> Sections);
