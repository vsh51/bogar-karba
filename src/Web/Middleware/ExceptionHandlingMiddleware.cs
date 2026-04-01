namespace Web.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
#pragma warning disable S2139 // Log and rethrow is intentional: log here, UseExceptionHandler renders error page
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception occurred while processing {Path}", context.Request.Path);
            throw;
        }
#pragma warning restore S2139
    }
}
