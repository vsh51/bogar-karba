namespace Application.UseCases.GetChecklistForEdit;

public sealed record GetChecklistForEditResult(
    Guid Id,
    string Title,
    string Description,
    DateOnly? Deadline,
    List<EditSectionResult> Sections);
