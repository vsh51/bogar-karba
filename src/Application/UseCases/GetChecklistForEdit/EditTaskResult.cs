namespace Application.UseCases.GetChecklistForEdit;

public sealed record EditTaskResult(
    Guid Id,
    string Content);
