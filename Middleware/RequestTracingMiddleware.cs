using events_tickets.Models;
using events_tickets.Services;

namespace events_tickets.Middleware;

public sealed class RequestTracingMiddleware
{
    private const string CorrelationHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTracingMiddleware> _logger;

    public RequestTracingMiddleware(RequestDelegate next, ILogger<RequestTracingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        var traceId = ResolveTraceId(context);
        context.TraceIdentifier = traceId;
        context.Response.Headers[CorrelationHeader] = traceId;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["Path"] = context.Request.Path.Value ?? string.Empty
        });

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled request error with trace {TraceId}", traceId);

            await auditLogService.LogSystemErrorAsync(new SystemErrorEntry
            {
                TraceId = traceId,
                Path = context.Request.Path.Value ?? string.Empty,
                Method = context.Request.Method,
                ErrorType = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            }, context.RequestAborted);

            throw;
        }
    }

    private static string ResolveTraceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationHeader, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
