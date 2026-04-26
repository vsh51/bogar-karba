namespace Domain.Entities;

public enum ChecklistStatus
{
    Draft,
    Published,
    Archived
}

public class Checklist
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ChecklistStatus Status { get; set; } = ChecklistStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateOnly? Deadline { get; set; }

    public string UserId { get; set; } = string.Empty;

    public bool IsPublic { get; set; } = true;

    public List<Section> Sections { get; set; } = new();
}
