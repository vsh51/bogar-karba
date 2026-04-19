namespace Application.UseCases.CreateChecklist;

public record CreateTaskRequest(
    string Content,
    int Position);
