using Printify.Domain.PrintJobs;
using Printify.Domain.Printers;

namespace Printify.Application.Printing;

public interface IPrintJobSessionsOrchestrator
{
    Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct, bool skipBufferCheck = false);
    Task<IPrintJobSession?> GetSessionAsync(IPrinterChannel channel, CancellationToken ct);
    Task FeedAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken ct);
    Task CompleteAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct);
    Task InjectDocumentAsync(Printer printer, PrinterSettings settings, ReadOnlyMemory<byte> data, CancellationToken ct);
}
