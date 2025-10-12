using Printify.Domain.Users;

namespace Printify.Application.Interfaces;

public interface IUserRepository
{
    ValueTask<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    ValueTask<User?> GetByDisplayNameAsync(string displayName, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}
