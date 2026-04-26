namespace Domain.Entities;

public class ChecklistAccess
{
    public Guid ChecklistId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public Checklist Checklist { get; set; } = null!;
}
