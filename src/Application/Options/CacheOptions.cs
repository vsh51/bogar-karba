namespace Application.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int PublishedChecklistMinutes { get; init; } = 10;
}
