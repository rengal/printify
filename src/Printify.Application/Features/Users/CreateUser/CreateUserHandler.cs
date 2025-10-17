using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.CreateUser;

public sealed class CreateUserHandler(IUserRepository userRepository) : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            // Simplified idempotency: return the existing entity without reapplying side effects.
            return existing;
        }

        // NOTE: Simplified idempotency – we only rely on the supplied identifier.
        // We do not store the original response payload, so repeated calls could observe new fields if contracts evolve.

        // Generate the persisted user snapshot with the caller's network metadata for traceability.
        var user = new User(
            request.UserId,
            request.DisplayName,
            DateTimeOffset.UtcNow,
            request.Context.IpAddress,
            false);

        // Persist immediately so the user becomes visible for authentication flows.
        await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);

        return user;
    }
}
