using Printify.Domain.Documents;

namespace Printify.Domain.PrintJobs;

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
    Document? Document { get; }
    Task Feed(ReadOnlyMemory<byte> data, CancellationToken token);
    Task Complete(PrintJobCompletionReason reason);
}
