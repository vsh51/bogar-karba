namespace Web.Models.Checklist;

public class CreateChecklistViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<CreateSectionViewModel> Sections { get; set; } = new();
}
