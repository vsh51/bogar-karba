namespace Web.Options;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public int PollingIntervalSeconds { get; init; } = 10;
}
