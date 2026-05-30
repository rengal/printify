using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed class ListPrinterSidebarHandler(
    IPrinterRepository printerRepository,
    IWorkspaceRepository workspaceRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<ListPrinterSidebarQuery, PrinterSidebarListResponse>
{
    public async Task<PrinterSidebarListResponse> Handle(
        IReceiveContext<ListPrinterSidebarQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var workspaceId = PrinterAccess.RequireWorkspaceId(request.Context);
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace {workspaceId} not found.");

        var snapshots = await printerRepository
            .ListForSidebarAsync(workspaceId, workspace.Role == WorkspaceRole.Admin, cancellationToken)
            .ConfigureAwait(false);

        if (snapshots.Count == 0)
        {
            return new PrinterSidebarListResponse(snapshots);
        }

        var updatedSnapshots = snapshots
            .Select(snapshot =>
            {
                var runtimeStatus = runtimeStatusStore.Get(snapshot.Printer.Id);
                return snapshot with { RuntimeStatus = runtimeStatus };
            })
            .ToList();

        return new PrinterSidebarListResponse(updatedSnapshots);
    }
}

