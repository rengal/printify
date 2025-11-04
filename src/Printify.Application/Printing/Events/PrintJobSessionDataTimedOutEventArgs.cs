namespace Printify.Application.Printing.Events;

/// <summary>
/// Event payload for print job data timeout
/// </summary>
public sealed record PrintJobSessionDataTimedOutEventArgs(IPrinterChannel Channel, CancellationToken CancellationToken);
