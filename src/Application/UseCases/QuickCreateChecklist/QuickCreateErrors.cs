namespace Application.UseCases.QuickCreateChecklist;

public static class QuickCreateErrors
{
    public const string EmptyInput = "Input text is required.";
    public const string TitleRequired = "A title line starting with '# ' is required.";
    public const string TitleMustBeFirst = "Line {0}: title line '# ...' must come before any other content.";
    public const string MultipleTitles = "Line {0}: only one title line '# ...' is allowed.";
    public const string EmptyTitle = "Line {0}: title cannot be empty.";
    public const string EmptySectionName = "Line {0}: section name cannot be empty.";
    public const string EmptyTaskContent = "Line {0}: task content cannot be empty.";
    public const string UnrecognizedLine = "Line {0}: unrecognized format. Use '# Title', '## Section', or '- [ ] task'.";
    public const string CheckedItemNotAllowed = "Line {0}: items must be unchecked ('[ ]' or no brackets) — a new checklist starts with no progress.";
    public const string NoTasks = "At least one task is required.";
    public const string TitleTooLong = "Title is too long (max {0} characters).";
    public const string SectionNameTooLong = "Section name is too long (max {0} characters).";
}
