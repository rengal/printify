using System;
using System.Threading;
using System.Threading.Tasks;

namespace Printify.Application.Printing;

/// <summary>
/// Coordinates one or more printer listeners, exposing lifecycle and status operations.
/// </summary>
public interface IPrinterListenerOrchestrator
{
    Task AddListenerAsync(Guid printerId, IPrinterListener listener, CancellationToken cancellationToken);
    Task RemoveListenerAsync(Guid printerId, CancellationToken cancellationToken);
    ListenerStatusSnapshot GetStatus(Guid printerId);
}

public sealed record ListenerStatusSnapshot(Guid PrinterId, bool IsActive, DateTimeOffset CapturedAt);
