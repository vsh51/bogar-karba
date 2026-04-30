namespace Domain.Entities;

public class ChecklistCoAuthor
{
    public Guid ChecklistId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public bool WritingPermission { get; set; }

    public bool DeletePermission { get; set; }

    public bool ActivateTogglePermission { get; set; }

    public Checklist? Checklist { get; set; }
}
