namespace Application.Options;

public sealed class ChecklistOptions
{
    public const string SectionName = "Checklist";

    public int TitleMaxLength { get; init; } = 200;

    public int SectionNameMaxLength { get; init; } = 100;

    public int SearchMinLength { get; init; } = 2;
}
