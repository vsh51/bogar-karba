using System.Diagnostics;
using System.Globalization;

namespace Web.Middleware;

public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    private const long SlowRequestThresholdMs = 500;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time-Ms"] =
                stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });

        await next(context);

        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var method = context.Request.Method;
        var path = context.Request.Path;
        var status = context.Response.StatusCode;

        if (elapsedMs >= SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow request detected: {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                method,
                path,
                status,
                elapsedMs);
        }
        else
        {
            logger.LogInformation(
                "Request completed: {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                method,
                path,
                status,
                elapsedMs);
        }
    }
}
