using Printify.Domain.Printers;
using Printify.Domain.Users;

namespace Printify.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken ct);
    Task AddAsync(Printer printer, CancellationToken ct);
}
