namespace Application.UseCases.EditChecklist;

public record EditTaskRequest(
    Guid Id,
    string Content,
    string? Link = null);
