using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Interfaces;

namespace Printify.Application.Features.Workspaces.GetWorkspaceSummary;

public sealed class GetWorkspaceSummaryHandler(
    IWorkspaceRepository workspaceRepository,
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository)
    : IRequestHandler<GetWorkspaceSummaryQuery, WorkspaceSummary>
{
    public async Task<WorkspaceSummary> Handle(IReceiveContext<GetWorkspaceSummaryQuery> context, CancellationToken cancellationToken)
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

        var printers = await printerRepository.ListOwnedAsync(workspaceId, cancellationToken)
            .ConfigureAwait(false);

        var totalDocuments = await documentRepository.CountByWorkspaceIdAsync(workspaceId.Value, cancellationToken)
            .ConfigureAwait(false);

        var last24h = DateTimeOffset.UtcNow.AddHours(-24);
        var documentsLast24h = await documentRepository.CountByWorkspaceIdSinceAsync(workspaceId.Value, last24h, cancellationToken)
            .ConfigureAwait(false);

        var lastDocumentAt = await documentRepository.GetLastDocumentTimestampByWorkspaceIdAsync(workspaceId.Value, cancellationToken)
            .ConfigureAwait(false);

        return new WorkspaceSummary(
            TotalPrinters: printers.Count,
            TotalDocuments: totalDocuments,
            DocumentsLast24h: documentsLast24h,
            LastDocumentAt: lastDocumentAt,
            CreatedAt: workspace.CreatedAt);
    }
}

