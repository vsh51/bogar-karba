using System.Security.Claims;
using System.Text;

namespace Web.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly HashSet<string> MethodsWithBody = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var method = request.Method;
        var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var headers = new StringBuilder();
        foreach (var header in request.Headers)
        {
            headers.Append(header.Key).Append(": ").AppendLine(header.Value);
        }

        string? body = null;
        if (MethodsWithBody.Contains(method) && request.ContentLength is > 0)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogInformation(
            "HTTP {Method} {Url} | IP: {IpAddress} | User: {UserId} | Headers: {Headers} | Body: {Body}",
            method,
            url,
            ip,
            userId ?? "anonymous",
            headers.ToString().TrimEnd(),
            body ?? "(empty)");

        await next(context);
    }
}
