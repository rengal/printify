using Printify.Domain.Printers;

namespace Printify.Application.Interfaces;

public interface IPrinterRepository
{
    Task<Printer?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<Printer>> ListByUserAsync(long userId, CancellationToken ct);
    Task AddAsync(Printer printer, CancellationToken ct);
    Task UpdateAsync(Printer printer, CancellationToken ct);
    Task DeleteAsync(Printer printer, CancellationToken ct);
    Task<int> GetFreeTcpPortNumber(CancellationToken ct);
}
