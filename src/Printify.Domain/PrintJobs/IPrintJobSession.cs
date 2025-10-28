using Printify.Domain.Documents;

namespace Printify.Domain.PrintJobs;

/// <summary>
/// Represents an in-flight printing session bound to a particular <see cref="PrintJob"/>.
/// </summary>
public interface IPrintJobSession
{
    PrintJob Job { get; }
    int BytesReceived { get; }
    int SendBytes { get; }
    DateTimeOffset LastReceivedBytes { get; }
    bool IsBufferBusy { get; }
    bool HasOverflow { get; }
    Document? Document { get; }

    Task Feed(ReadOnlySpan<byte> data);
    Task Complete(PrintJobCompletionReason reason);
}
