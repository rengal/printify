using System.Globalization;
using Microsoft.AspNetCore.Http;
using Printify.Application.Sessions;
using Printify.Domain.Sessions;

namespace Printify.Web.Infrastructure.Sessions;

/// <summary>
/// Writes session cookies using the current <see cref="HttpContext"/>.
/// </summary>
public sealed class HttpContextSessionCookieWriter : ISessionCookieWriter
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpContextSessionCookieWriter(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        this.httpContextAccessor = httpContextAccessor;
    }

    public void SetSessionCookie(long sessionId, DateTimeOffset expiresAt)
    {
        var context = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is not available.");

        context.Response.Cookies.Append(
            SessionDefaults.SessionCookieName,
            sessionId.ToString(CultureInfo.InvariantCulture),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = expiresAt,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
    }

    public void ClearSessionCookie()
    {
        var context = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is not available.");
        context.Response.Cookies.Delete(SessionDefaults.SessionCookieName);
    }
}
