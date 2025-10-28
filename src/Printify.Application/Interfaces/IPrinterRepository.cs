using Printify.Domain.Printers;

namespace Printify.Application.Interfaces;

public interface IPrinterRepository
{
    ValueTask<Printer?> GetByIdAsync(Guid id, CancellationToken ct);
    ValueTask<Printer?> GetByIdAsync(Guid id, Guid? ownerUserId, Guid? ownerSessionId, CancellationToken ct);
    ValueTask<IReadOnlyList<Printer>> ListAllAsync(CancellationToken ct);
    ValueTask<IReadOnlyList<Printer>> ListOwnedAsync(Guid? ownerUserId, Guid? ownerSessionId, CancellationToken ct);
    ValueTask<Guid> AddAsync(Printer printer, CancellationToken ct);
    Task UpdateAsync(Printer printer, CancellationToken ct);
    Task DeleteAsync(Printer printer, CancellationToken ct);
    Task SetPinnedAsync(Guid id, Guid? ownerUserId, Guid? ownerSessionId, bool isPinned, CancellationToken ct);
    ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct);
}
