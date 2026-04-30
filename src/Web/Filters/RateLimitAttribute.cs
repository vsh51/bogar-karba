using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Web.Options;

namespace Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RateLimitAttribute : Attribute, IAsyncActionFilter
{
    public RateLimitAttribute(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name must be provided.", nameof(policyName));
        }

        PolicyName = policyName;
    }

    public string PolicyName { get; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var services = context.HttpContext.RequestServices;
        var options = services.GetRequiredService<IOptionsMonitor<RateLimitOptions>>().CurrentValue;

        if (!options.Policies.TryGetValue(PolicyName, out var policy))
        {
            throw new InvalidOperationException(
                $"Rate limit policy '{PolicyName}' is not configured under '{RateLimitOptions.SectionName}:Policies'.");
        }

        if (policy.MaxRequests <= 0 || policy.WindowSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"Rate limit policy '{PolicyName}' must have positive MaxRequests and WindowSeconds.");
        }

        var cache = services.GetRequiredService<IMemoryCache>();

        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var endpoint = context.ActionDescriptor.DisplayName ?? context.ActionDescriptor.Id;
        var cacheKey = $"rate-limit:{PolicyName}:{endpoint}:{ipAddress}";

        var counter = cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(policy.WindowSeconds);
            return new RateLimitCounter();
        })!;

        var current = counter.Increment();

        if (current > policy.MaxRequests)
        {
            var logger = services.GetRequiredService<ILogger<RateLimitAttribute>>();
            logger.LogWarning("Rate limit exceeded for {IpAddress} on {PolicyName}", ipAddress, PolicyName);

            context.Result = new RedirectToActionResult("RateLimitExceeded", "Home", null);
            return;
        }

        await next();
    }

    private sealed class RateLimitCounter
    {
        private int _count;

        public int Increment() => Interlocked.Increment(ref _count);
    }
}
