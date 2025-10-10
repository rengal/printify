using System.Globalization;
using Microsoft.AspNetCore.Http;
using Printify.Domain.Requests;
using Printify.Domain.Sessions;

namespace Printify.Web.Infrastructure;

internal static class HttpContextExtensions
{
    internal const string IdempotencyKeyHeader = "Idempotency-Key";
    internal const string IdempotencyKeyItemName = "IdempotencyKey";

    internal static RequestContext CaptureRequestContext(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ipAddress = context.GetClientIpAddress();
        var idempotencyKey = context.GetIdempotencyKey();
        var sessionId = context.TryGetSessionIdFromCookie();

        if (idempotencyKey is not null)
        {
            context.Items[IdempotencyKeyItemName] = idempotencyKey;
        }

        return new RequestContext(sessionId, null, ipAddress, idempotencyKey, null);
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

    private static long? TryGetSessionIdFromCookie(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Cookies.TryGetValue(SessionDefaults.SessionCookieName, out var cookieValue))
        {
            return null;
        }

        return long.TryParse(cookieValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sessionId)
            ? sessionId
            : null;
    }
}
