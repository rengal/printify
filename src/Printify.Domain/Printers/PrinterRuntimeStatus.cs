namespace Printify.Domain.Printers;

using Mediator.Net.Contracts;

/// <summary>
/// Runtime-only printer state computed by the listener; never persisted.
/// Add fields here only when they are derived from active runtime behavior.
/// Null fields indicate no change and should not overwrite existing state.
/// </summary>
public sealed record PrinterRuntimeStatus(
    Guid PrinterId,
    PrinterState? State = null,
    DateTimeOffset? UpdatedAt = null,
    int? BufferedBytes = null,
    int? BufferedBytesDeltaBps = null,
    DrawerState? Drawer1State = null,
    DrawerState? Drawer2State = null) : IResponse;

