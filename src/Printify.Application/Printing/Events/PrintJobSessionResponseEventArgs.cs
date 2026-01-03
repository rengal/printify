namespace Printify.Application.Printing.Events;

/// <summary>
/// Event payload for printer responses that need to be sent back to the client.
/// </summary>
public sealed record PrintJobSessionResponseEventArgs(ReadOnlyMemory<byte> Data, CancellationToken CancellationToken);
