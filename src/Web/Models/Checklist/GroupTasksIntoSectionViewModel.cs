namespace Web.Models.Checklist;

public sealed class GroupTasksIntoSectionViewModel
{
    public string SectionName { get; set; } = string.Empty;

    public List<Guid> TaskIds { get; set; } = new();
}
