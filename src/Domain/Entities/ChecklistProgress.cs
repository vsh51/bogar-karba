namespace Domain.Entities;

public sealed class ChecklistProgress
{
    public Guid ChecklistId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string CompletedTaskIdsJson { get; set; } = "[]";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
