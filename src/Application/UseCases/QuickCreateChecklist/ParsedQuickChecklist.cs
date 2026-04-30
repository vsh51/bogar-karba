namespace Application.UseCases.QuickCreateChecklist;

public sealed record ParsedQuickChecklist(
    string Title,
    string Description,
    List<ParsedQuickSection> Sections);
