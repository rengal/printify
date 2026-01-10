using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.UpdateWorkspace;

public sealed class UpdateWorkspaceHandler(
    IWorkspaceRepository workspaceRepository)
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
            if (request.DocumentRetentionDays.Value < 1 || request.DocumentRetentionDays.Value > 365)
            {
                throw new ArgumentException("DocumentRetentionDays must be between 1 and 365");
            }
        }

        // Update workspace with new values
        var updated = workspace with
        {
            Name = request.Name ?? workspace.Name,
            DocumentRetentionDays = request.DocumentRetentionDays ?? workspace.DocumentRetentionDays
        };

        await workspaceRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        return updated;
    }
}
