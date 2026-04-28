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
    public const string UserAlreadyHasAccess = "This user already has access to the checklist.";
    public const string CannotGrantAccessToOwner = "Cannot grant access to the checklist owner.";
    public const string BoredApiUnavailable = "Could not fetch activity. Please try again.";
}
