using Printify.Domain.Printers;

namespace Printify.Application.Interfaces;

public interface IPrinterRepository
{
    ValueTask<Printer?> GetByIdAsync(Guid id, CancellationToken ct);
    ValueTask<Printer?> GetByIdAsync(Guid id, Guid? workspaceId, CancellationToken ct);
    ValueTask<IReadOnlyList<Printer>> ListAllAsync(CancellationToken ct);
    ValueTask<IReadOnlyList<Printer>> ListOwnedAsync(Guid? workspaceId, CancellationToken ct);
    ValueTask AddAsync(Printer printer, CancellationToken ct);
    Task UpdateAsync(Printer printer, CancellationToken ct);
    Task DeleteAsync(Printer printer, CancellationToken ct);
    Task SetPinnedAsync(Guid id, bool isPinned, CancellationToken ct);
    Task SetLastDocumentReceivedAtAsync(Guid id, DateTimeOffset timestamp, CancellationToken ct);
    Task SetDesiredStatusAsync(Guid id, PrinterDesiredStatus desiredStatus, CancellationToken ct);
    Task SetRuntimeStatusAsync(Guid id, PrinterRuntimeStatus runtimeStatus, DateTimeOffset updatedAt, string? error, CancellationToken ct);
    ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct);
}
