namespace Web.Models.Checklist;

public class CreateSectionViewModel
{
    public string Name { get; set; } = string.Empty;

    public int Position { get; set; }

    public List<CreateTaskViewModel> Tasks { get; set; } = new();
}
