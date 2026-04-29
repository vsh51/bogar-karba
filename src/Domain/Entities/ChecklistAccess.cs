namespace Domain.Entities;

public class ChecklistAccess
{
    public Guid ChecklistId { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>True for the original creator; false for collaborators added via Share.</summary>
    public bool IsOwner { get; set; }

    public Checklist Checklist { get; set; } = null!;
}
