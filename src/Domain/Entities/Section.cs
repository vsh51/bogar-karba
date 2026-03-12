namespace Domain.Entities;

public class Section
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int Position { get; set; }

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    public List<TaskItem> Tasks { get; set; } = new();
}