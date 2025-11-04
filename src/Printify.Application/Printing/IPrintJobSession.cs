using Printify.Application.Printing.Events;
using Printify.Domain.Documents;
using Printify.Domain.PrintJobs;

namespace Printify.Application.Printing;

/// <summary>
/// Represents a printing session bound to a particular <see cref="PrintJob"/>.
/// </summary>
public interface IPrintJobSession
{
    int TotalBytesReceived { get; }
    int TotalBytesSent { get; }
    DateTimeOffset LastReceivedBytes { get; }
    bool IsBufferBusy { get; }
    bool HasOverflow { get; }
    bool IsCompleted { get; }
    Document? Document { get; }
    Task Feed(ReadOnlyMemory<byte> data, CancellationToken ct);
    Task Complete(PrintJobCompletionReason reason);

    /// <summary>
    /// Raised once when no data has been received from the client within the expected timeout period,
    /// indicating the print job session may be completed or stalled
    /// </summary>
    event Func<IPrintJobSession, PrintJobSessionDataTimedOutEventArgs, ValueTask>? DataTimedOut;
}
