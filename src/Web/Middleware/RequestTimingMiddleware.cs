using System.Diagnostics;
using System.Globalization;

namespace Web.Middleware;

/// <summary>
/// Middleware that measures and logs the execution time of every HTTP request.
/// Slow requests (exceeding <see cref="SlowRequestThresholdMs"/> ms) are logged
/// at Warning level so they are easy to spot in structured logging systems such as Seq.
/// The elapsed time is also exposed via the <c>X-Response-Time-Ms</c> response header
/// for client-side diagnostics.
/// </summary>
public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    /// <summary>Requests that take longer than this value are logged at Warning level.</summary>
    private const long SlowRequestThresholdMs = 500;

    /// <summary>Measures request execution time and logs it via structured logging.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
