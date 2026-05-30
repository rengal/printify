using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Printers.List;

public sealed class ListPrintersHandler(
    IPrinterRepository printerRepository,
    IWorkspaceRepository workspaceRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<ListPrintersQuery, PrinterListResponse>
{
    public async Task<PrinterListResponse> Handle(
        IReceiveContext<ListPrintersQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var workspaceId = PrinterAccess.RequireWorkspaceId(request.Context);
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace {workspaceId} not found.");

        var printers = workspace.Role == WorkspaceRole.Admin
            ? await printerRepository.ListAllAsync(cancellationToken).ConfigureAwait(false)
            : await printerRepository.ListOwnedAsync(workspaceId, cancellationToken).ConfigureAwait(false);

        if (workspace.Role == WorkspaceRole.Admin)
        {
            var ownedPrinters = printers.Where(printer => printer.OwnerWorkspaceId == workspaceId);
            var foreignPrinters = printers
                .Where(printer => printer.OwnerWorkspaceId != workspaceId)
                .OrderByDescending(printer => printer.LastDocumentReceivedAt.HasValue)
                .ThenByDescending(printer => printer.LastDocumentReceivedAt)
                .ThenBy(printer => printer.DisplayName);
            printers = ownedPrinters.Concat(foreignPrinters).ToList();
        }

        if (printers.Count == 0)
        {
            return new PrinterListResponse([]);
        }

        var printerIds = printers.Select(printer => printer.Id).ToList();
        var flags = await printerRepository
            .ListOperationalFlagsByPrinterIdsAsync(printerIds, cancellationToken)
            .ConfigureAwait(false);
        var settings = await printerRepository
            .ListSettingsByPrinterIdsAsync(printerIds, cancellationToken)
            .ConfigureAwait(false);
        var ownerWorkspaceNames = await GetOwnerWorkspaceNamesAsync(
                printers,
                workspaceRepository,
                cancellationToken)
            .ConfigureAwait(false);

        var snapshots = printers
            .Select(printer =>
            {
                flags.TryGetValue(printer.Id, out var operationalFlags);
                // Settings are persisted separately; missing settings indicate a data integrity issue.
                if (!settings.TryGetValue(printer.Id, out var printerSettings))
                {
                    throw new InvalidOperationException($"Settings for printer {printer.Id} are missing.");
                }
                var runtimeStatus = runtimeStatusStore.Get(printer.Id);
                return new PrinterDetailsSnapshot(
                    printer,
                    printerSettings,
                    operationalFlags,
                    runtimeStatus,
                    ownerWorkspaceNames.GetValueOrDefault(printer.OwnerWorkspaceId));
            })
            .ToList();

        return new PrinterListResponse(snapshots);
    }

    private static async ValueTask<IReadOnlyDictionary<Guid, string>> GetOwnerWorkspaceNamesAsync(
        IReadOnlyCollection<Printer> printers,
        IWorkspaceRepository workspaceRepository,
        CancellationToken cancellationToken)
    {
        var workspaceNames = new Dictionary<Guid, string>();
        foreach (var ownerWorkspaceId in printers.Select(printer => printer.OwnerWorkspaceId).Distinct())
        {
            var ownerWorkspace = await workspaceRepository.GetByIdAsync(ownerWorkspaceId, cancellationToken)
                .ConfigureAwait(false);
            if (ownerWorkspace is not null)
            {
                workspaceNames[ownerWorkspaceId] = ownerWorkspace.Name;
            }
        }

        return workspaceNames;
    }
}

