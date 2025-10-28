using System;
using Printify.Application.Printing;

namespace Printify.Application.Printing.Events;

/// <summary>
/// Event payload describing a newly accepted printer channel.
/// </summary>
public sealed record PrinterChannelAcceptedEventArgs(Guid PrinterId, IPrinterChannel Channel);
