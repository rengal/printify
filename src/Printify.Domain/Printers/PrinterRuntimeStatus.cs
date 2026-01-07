namespace Printify.Domain.Printers;

using Mediator.Net.Contracts;

/// <summary>
/// Runtime-only printer state computed by the listener; never persisted.
/// Add fields here only when they are derived from active runtime behavior.
/// </summary>
public sealed record PrinterRuntimeStatus(
    Guid PrinterId,
    PrinterState State,
    DateTimeOffset UpdatedAt,
    int BufferedBytes,
    DrawerState Drawer1State,
    DrawerState Drawer2State) : IResponse;

