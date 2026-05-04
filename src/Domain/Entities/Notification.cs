namespace Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string EventKey { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsSent { get; set; }
}
