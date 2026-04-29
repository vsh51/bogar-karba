namespace Web.Options;

public sealed class RateLimitPolicy
{
    public int MaxRequests { get; init; }

    public int WindowSeconds { get; init; } = 60;
}
