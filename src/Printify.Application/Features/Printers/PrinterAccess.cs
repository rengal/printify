using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Domain.Requests;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Printers;

internal static class PrinterAccess
{
    internal static Guid RequireWorkspaceId(RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        return context.WorkspaceId.Value;
    }

    internal static async ValueTask<Workspace> GetCurrentWorkspaceAsync(
        IWorkspaceRepository workspaceRepository,
        RequestContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(workspaceRepository);

        var workspaceId = RequireWorkspaceId(context);
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId, ct).ConfigureAwait(false);
        if (workspace is null)
        {
            throw new BadRequestException("Workspace could not be resolved.");
        }

        return workspace;
    }

    internal static async ValueTask<Printer?> GetReadablePrinterAsync(
        IPrinterRepository printerRepository,
        IWorkspaceRepository workspaceRepository,
        RequestContext context,
        Guid printerId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printerRepository);

        var workspace = await GetCurrentWorkspaceAsync(workspaceRepository, context, ct).ConfigureAwait(false);
        var workspaceId = RequireWorkspaceId(context);

        // Admin workspaces can inspect printers across the whole installation.
        return workspace.Role == WorkspaceRole.Admin
            ? await printerRepository.GetByIdAsync(printerId, ct).ConfigureAwait(false)
            : await printerRepository.GetByIdAsync(printerId, workspaceId, ct).ConfigureAwait(false);
    }

    internal static async ValueTask<Printer> GetWritablePrinterAsync(
        IPrinterRepository printerRepository,
        RequestContext context,
        Guid printerId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printerRepository);

        var workspaceId = RequireWorkspaceId(context);
        var printer = await printerRepository.GetByIdAsync(printerId, ct).ConfigureAwait(false);
        if (printer is null)
        {
            throw new PrinterNotFoundException(printerId);
        }

        // Cross-workspace mutations are forbidden even for admin workspaces.
        if (printer.OwnerWorkspaceId != workspaceId)
        {
            throw new ForbiddenException("This printer belongs to another workspace and is read-only.");
        }

        return printer;
    }
}
