namespace Application.UseCases.QuickCreateChecklist;

public sealed record ParsedQuickSection(
    string Name,
    int Position,
    List<string> Tasks);
