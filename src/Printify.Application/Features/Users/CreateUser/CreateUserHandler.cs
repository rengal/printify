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

        // Generate the persisted user snapshot with the caller's network metadata for traceability.
        var user = new User(
            Guid.NewGuid(),
            request.DisplayName,
            DateTimeOffset.UtcNow,
            request.Context.IpAddress,
            false);

        // Persist immediately so the user becomes visible for authentication flows.
        await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);

        return user;
    }
}
