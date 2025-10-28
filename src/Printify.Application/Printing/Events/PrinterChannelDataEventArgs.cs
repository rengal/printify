using System;
using System.Threading;

namespace Printify.Application.Printing.Events;

/// <summary>
/// Event payload for channel data reception.
/// </summary>
public sealed record PrinterChannelDataEventArgs(ReadOnlyMemory<byte> Buffer, CancellationToken CancellationToken);
