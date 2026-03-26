namespace Application.UseCases.CreateChecklist;

public record CreateChecklistRequest(
    string Title,
    string Description,
    List<CreateSectionRequest> Sections);
