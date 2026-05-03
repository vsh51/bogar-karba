namespace Web.Options;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public Dictionary<string, RateLimitPolicy> Policies { get; init; }
        = new(StringComparer.OrdinalIgnoreCase);
}
