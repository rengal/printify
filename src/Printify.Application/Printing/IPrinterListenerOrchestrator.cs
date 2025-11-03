using System;
using System.Collections.Generic;
using System.Threading;
using Printify.Domain.Printers;

namespace Printify.Application.Printing;

/// <summary>
/// Coordinates one or more printer listeners, exposing lifecycle and status operations.
/// </summary>
public interface IPrinterListenerOrchestrator
{
    Task AddListenerAsync(Printer printer, CancellationToken ct);
    Task RemoveListenerAsync(Printer printer, CancellationToken ct);
    ListenerStatusSnapshot? GetStatus(Printer printer);
    IReadOnlyCollection<IPrinterChannel> GetActiveChannels(Guid printerId);
}

public sealed record ListenerStatusSnapshot(PrinterListenerStatus Status);
