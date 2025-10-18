using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Session;
using Printify.Domain.Requests;

namespace Printify.Web.Infrastructure;

internal static class HttpContextExtensions
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    internal static RequestContext CaptureRequestContext(this HttpContext httpContext)
    {
        var user = httpContext.User;
        Guid? anonymousSessionId = null;
        Guid? userId = null;

        // 1. Try JWT claims (if authenticated)
        if (user?.Identity?.IsAuthenticated == true)
        {
            if (Guid.TryParse(user.FindFirstValue("sessionId"), out var sid))
                anonymousSessionId = sid;

            if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
                userId = uid;
        }

        // 2. Try fallback via session repository if needed
        //    (optional � depends on how anonymous sessions are managed)
        // anonymousSessionId ??= sessionRepository.GetCurrentAnonymousSessionId(HttpContext);

        // 3. Idempotency Key from headers
        httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader);
        var idempotencyKey = keyHeader.FirstOrDefault();

        // 4. Client IP address
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ipAddress) &&
            httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var forwardedValue = forwardedFor.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedValue))
            {
                ipAddress = forwardedValue.Split(',')[0].Trim();
            }
        }

        return new RequestContext(anonymousSessionId, userId, ipAddress, idempotencyKey);
    }

    internal static string GetClientIpAddress(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string? GetIdempotencyKey(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
