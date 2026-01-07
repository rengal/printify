using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed class ListPrinterSidebarHandler(
    IPrinterRepository printerRepository,
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

        if (request.Context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        var snapshots = await printerRepository
            .ListForSidebarAsync(request.Context.WorkspaceId.Value, cancellationToken)
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

