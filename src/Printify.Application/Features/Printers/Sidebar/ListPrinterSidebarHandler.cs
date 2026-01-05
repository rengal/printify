using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed class ListPrinterSidebarHandler(
    IPrinterRepository printerRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<ListPrinterSidebarQuery, IReadOnlyList<PrinterSidebarSnapshot>>
{
    public async Task<IReadOnlyList<PrinterSidebarSnapshot>> Handle(
        ListPrinterSidebarQuery request,
        CancellationToken cancellationToken)
    {
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
            return snapshots;
        }

        return snapshots
            .Select(snapshot =>
            {
                var runtimeStatus = runtimeStatusStore.Get(snapshot.Printer.Id);
                return snapshot with { RuntimeStatus = runtimeStatus };
            })
            .ToList();
    }
}
