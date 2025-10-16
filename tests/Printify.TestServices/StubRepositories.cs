using System;
using System.Threading;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Domain.Users;

namespace Printify.TestServices;

public sealed class NullUserRepository : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken)
        => Task.FromException(new InvalidOperationException("User repository should not be used in this scenario."));

    public ValueTask<User?> GetByDisplayNameAsync(string displayName, CancellationToken cancellationToken)
        => ValueTask.FromResult<User?>(null);

    public ValueTask<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => ValueTask.FromResult<User?>(null);
}

public sealed class NullPrinterRepository : IPrinterRepository
{
    public Task DeleteAsync(Printer printer, CancellationToken ct)
        => Task.FromException(new InvalidOperationException("Printer repository should not be used in this scenario."));

    public ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct)
        => ValueTask.FromResult(0);

    public ValueTask<Printer?> GetByIdAsync(Guid id, CancellationToken ct)
        => ValueTask.FromResult<Printer?>(null);

    public ValueTask<IReadOnlyList<Printer>> ListByUserAsync(Guid userId, CancellationToken ct)
        => ValueTask.FromResult<IReadOnlyList<Printer>>(Array.Empty<Printer>());

    public ValueTask<Guid> AddAsync(Printer printer, CancellationToken ct)
        => ValueTask.FromResult(Guid.Empty);

    public Task UpdateAsync(Printer printer, CancellationToken ct)
        => Task.FromException(new InvalidOperationException("Printer repository should not be used in this scenario."));
}
