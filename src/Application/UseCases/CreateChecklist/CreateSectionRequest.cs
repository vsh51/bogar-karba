namespace Application.UseCases.CreateChecklist;

public record CreateSectionRequest(
    string Name,
    int Position,
    List<CreateTaskRequest> Tasks);
