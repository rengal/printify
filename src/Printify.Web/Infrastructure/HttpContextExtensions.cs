using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Printify.Domain.Requests;

namespace Printify.Web.Infrastructure;

internal static class HttpContextExtensions
{
    internal static RequestContext CaptureRequestContext(this HttpContext httpContext)
    {
        var user = httpContext.User;
        Guid? workspaceId = null;

        //todo debugnow
        System.Diagnostics.Debug.WriteLine($"User Identity: {user?.Identity?.Name}");
        System.Diagnostics.Debug.WriteLine($"Is Authenticated: {user?.Identity?.IsAuthenticated}");
        System.Diagnostics.Debug.WriteLine($"Authentication Type: {user?.Identity?.AuthenticationType}");
        System.Diagnostics.Debug.WriteLine($"Claims Count: {user?.Claims?.Count()}");

        // Log all claims to see what's actually there
        if (user?.Claims != null)
        {
            foreach (var claim in user.Claims)
            {
                System.Diagnostics.Debug.WriteLine($"  Claim Type: '{claim.Type}' = Value: '{claim.Value}'");
            }
        }

        // 1. Try JWT claims (if authenticated)
        if (user?.Identity?.IsAuthenticated == true)
        {
            if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var wid))
                workspaceId = wid;
        }

        // 2. Client IP address
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = "127.0.0.1";
        }

        return new RequestContext(workspaceId, ipAddress);
    }
}
