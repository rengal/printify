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
                return new RequestContext(null, false, ipAddress);
        }

        return new RequestContext(workspaceId, true, ipAddress);
    }
}
