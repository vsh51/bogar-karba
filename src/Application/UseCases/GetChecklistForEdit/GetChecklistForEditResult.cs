namespace Application.UseCases.GetChecklistForEdit;

public sealed record GetChecklistForEditResult(
    Guid Id,
    string Title,
    string Description,
    List<EditSectionResult> Sections);
