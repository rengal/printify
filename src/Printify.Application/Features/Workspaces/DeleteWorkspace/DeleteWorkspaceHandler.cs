using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Interfaces;

namespace Printify.Application.Features.Workspaces.DeleteWorkspace;

public sealed class DeleteWorkspaceHandler(
    IWorkspaceRepository workspaceRepository)
    : IRequestHandler<DeleteWorkspaceCommand, DeleteWorkspaceResult>
{
    public async Task<DeleteWorkspaceResult> Handle(IReceiveContext<DeleteWorkspaceCommand> context, CancellationToken cancellationToken)
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

        // Hard delete: Permanently removes the workspace, all printers, and all documents
        await workspaceRepository.DeleteAsync(workspaceId.Value, cancellationToken).ConfigureAwait(false);

        return new DeleteWorkspaceResult();
    }
}
