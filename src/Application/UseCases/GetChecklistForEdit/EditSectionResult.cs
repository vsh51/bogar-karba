namespace Application.UseCases.GetChecklistForEdit;

public sealed record EditSectionResult(
    Guid Id,
    string Name,
    List<EditTaskResult> Tasks);
