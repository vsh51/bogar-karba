namespace Application.UseCases.CreateChecklist;

public record CreateChecklistCommand(
    string Title,
    string Description,
    DateOnly? Deadline,
    List<CreateSectionRequest> Sections);
