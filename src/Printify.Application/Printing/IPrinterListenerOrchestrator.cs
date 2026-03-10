using Printify.Domain.Printers;
using Printify.Domain.Workspaces;

namespace Printify.Application.Printing;

/// <summary>
/// Coordinates one or more printer listeners, exposing lifecycle and status operations.
/// </summary>
public interface IPrinterListenerOrchestrator
{
    Task AddListenerAsync(Printer printer, PrinterSettings settings, Workspace workspace, PrinterTargetState targetState, CancellationToken ct);
    Task RemoveListenerAsync(Printer printer, PrinterTargetState targetState, CancellationToken ct);
    /// <summary>Updates the cached workspace snapshot so running listeners pick up whitelist changes immediately.</summary>
    void UpdateWorkspace(Workspace workspace);
    ListenerStatusSnapshot GetStatus(Printer printer);
    IReadOnlyCollection<IPrinterChannel> GetActiveChannels(Guid printerId);
}

public sealed record ListenerStatusSnapshot(PrinterListenerStatus Status);
