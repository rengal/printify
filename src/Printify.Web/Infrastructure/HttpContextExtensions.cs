using Printify.Application.Interfaces;
using Printify.Domain.Requests;
using System.Security.Claims;

namespace Printify.Web.Infrastructure;

public class HttpContextExtensions(IServiceProvider serviceProvider)
{
    public async ValueTask<RequestContext> CaptureRequestContext(HttpContext httpContext)
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

        // 3. Check that workspace exists
        if (workspaceId != null)
        {
            using var scope = serviceProvider.CreateScope();
            var workspaceRepository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
            var workspace = await workspaceRepository.GetByIdAsync(workspaceId.Value, CancellationToken.None);
            if (workspace == null)
                return new RequestContext(null, ipAddress);
        }

        return new RequestContext(workspaceId, ipAddress);
    }
}
