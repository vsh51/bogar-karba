namespace Web.Models.Checklist;

public class EditSectionViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<EditTaskViewModel> Tasks { get; set; } = new();
}
