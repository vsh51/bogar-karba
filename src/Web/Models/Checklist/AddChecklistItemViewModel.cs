namespace Web.Models.Checklist;

public sealed class AddChecklistItemViewModel
{
    public required Guid SectionId { get; set; }

    public string Content { get; set; } = string.Empty;
}
