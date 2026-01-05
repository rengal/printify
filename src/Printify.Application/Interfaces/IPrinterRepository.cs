using Printify.Domain.Printers;

namespace Printify.Application.Interfaces;

public interface IPrinterRepository
{
    ValueTask<Printer?> GetByIdAsync(Guid id, CancellationToken ct);
    ValueTask<Printer?> GetByIdAsync(Guid id, Guid? workspaceId, CancellationToken ct);
    ValueTask<IReadOnlyList<Printer>> ListAllAsync(CancellationToken ct);
    ValueTask<IReadOnlyList<Printer>> ListOwnedAsync(Guid? workspaceId, CancellationToken ct);
    ValueTask<IReadOnlyList<PrinterSidebarSnapshot>> ListForSidebarAsync(Guid workspaceId, CancellationToken ct);
    ValueTask AddAsync(Printer printer, PrinterSettings settings, CancellationToken ct);
    Task UpdateAsync(Printer printer, PrinterSettings settings, CancellationToken ct);
    Task DeleteAsync(Printer printer, CancellationToken ct);
    Task SetPinnedAsync(Guid id, bool isPinned, CancellationToken ct);
    Task SetLastDocumentReceivedAtAsync(Guid id, DateTimeOffset timestamp, CancellationToken ct);
    ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct);
    ValueTask<PrinterOperationalFlags?> GetOperationalFlagsAsync(Guid printerId, CancellationToken ct);
    ValueTask<PrinterSettings?> GetSettingsAsync(Guid printerId, CancellationToken ct);
    ValueTask<IReadOnlyDictionary<Guid, PrinterOperationalFlags>> ListOperationalFlagsAsync(Guid workspaceId, CancellationToken ct);
    ValueTask<IReadOnlyDictionary<Guid, PrinterSettings>> ListSettingsAsync(Guid workspaceId, CancellationToken ct);
    Task UpsertOperationalFlagsAsync(PrinterOperationalFlagsUpdate update, CancellationToken ct);
}
