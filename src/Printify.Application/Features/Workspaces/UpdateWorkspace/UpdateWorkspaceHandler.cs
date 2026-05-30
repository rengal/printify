using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.UpdateWorkspace;

public sealed class UpdateWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<UpdateWorkspaceCommand, Workspace>
{
    public async Task<Workspace> Handle(IReceiveContext<UpdateWorkspaceCommand> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var workspaceId = request.Context.WorkspaceId;
        if (!workspaceId.HasValue)
        {
            throw new InvalidOperationException("WorkspaceId is required");
        }

        var workspace = await workspaceRepository.GetByIdAsync(workspaceId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (workspace is null)
        {
            throw new InvalidOperationException("Workspace not found");
        }

        // Validate DocumentRetentionDays if provided
        if (request.DocumentRetentionDays.HasValue)
        {
            if (request.DocumentRetentionDays.Value < 0 || request.DocumentRetentionDays.Value > 365)
            {
                throw new ArgumentException("DocumentRetentionDays must be between 0 and 365");
            }
        }

        // Update workspace with new values
        var updated = workspace with
        {
            Name = request.Name ?? workspace.Name,
            Role = workspace.Role,
            DocumentRetentionDays = request.DocumentRetentionDays ?? workspace.DocumentRetentionDays,
            TcpWhitelistEnabled = request.TcpWhitelistEnabled ?? workspace.TcpWhitelistEnabled,
            TcpWhitelistEntries = request.TcpWhitelistEntries ?? workspace.TcpWhitelistEntries
        };

        await workspaceRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        // Propagate whitelist changes to any running TCP listeners immediately.
        listenerOrchestrator.UpdateWorkspace(updated);

        return updated;
    }
}
