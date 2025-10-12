using Printify.Domain.Printers;

namespace Printify.Application.Interfaces;

public interface IPrinterRepository
{
    ValueTask<Printer?> GetByIdAsync(Guid id, CancellationToken ct);
    ValueTask<IReadOnlyList<Printer>> ListByUserAsync(Guid userId, CancellationToken ct);
    ValueTask<Guid> AddAsync(Printer printer, CancellationToken ct);
    Task UpdateAsync(Printer printer, CancellationToken ct);
    Task DeleteAsync(Printer printer, CancellationToken ct);
    ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct);
}
