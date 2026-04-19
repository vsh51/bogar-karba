namespace Application.UseCases.EditChecklist;

public record EditSectionRequest(
    Guid Id,
    string Name,
    List<EditTaskRequest> Tasks);
