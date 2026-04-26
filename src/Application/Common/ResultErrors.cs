namespace Application.Common;

public static class ResultErrors
{
    public const string UserNotFound = "User not found.";
    public const string ChecklistNotFound = "Checklist not found.";
    public const string ChecklistIsPrivate = "This checklist is private.";
    public const string NotChecklistOwner = "You can only modify your own checklists.";
    public const string TitleRequired = "Title is required.";
    public const string SectionNotFound = "Section not found.";
    public const string ItemContentRequired = "Item content is required.";
    public const string AddingSectionsNotAllowed = "Adding new sections is not allowed.";
    public const string AddingTasksNotAllowed = "Adding new tasks is not allowed.";
    public const string DeadlineInPast = "Deadline cannot be earlier than today.";
    public const string DeadlineTooFar = "Deadline is too far in the future.";
}
