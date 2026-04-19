namespace Web.Models.Checklist;

public class EditChecklistViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateOnly? Deadline { get; set; }

    public List<EditSectionViewModel> Sections { get; set; } = new();
}
